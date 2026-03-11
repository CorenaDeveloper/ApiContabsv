using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IImportarDteService
    {
        Task<ImportarDteMasivoResponse> ValidarDocumentos(ImportarDteMasivoRequest request);
        Task<ImportarDteMasivoResponse> ValidarYGuardar(ConfirmarImportacionRequest request);
    }

    public class ImportarDteService : IImportarDteService
    {
        private readonly ContabilidadContext _contabilidadContext;
        private readonly ILogger<ImportarDteService> _logger;
        private readonly dteContext _dteContext;

        // IHaciendaService ya no se usa aquí — se eliminó la dependencia
        // Si en el futuro quieres re-activar consulta a Hacienda, agrégalo de nuevo

        private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "03", "05", "06"
        };

        private const int DEFAULT_TIPO_OPERACION = 1;
        private const int DEFAULT_CLASIFICACION = 1;
        private const int DEFAULT_TIPO_COSTO_GASTO = 1;
        private const int DEFAULT_SECTOR = 1;
        private const int DEFAULT_CLASE_DOCUMENTO = 1;
        private const int DEFAULT_TIPO_DOCUMENTO = 3;

        public ImportarDteService(
            ContabilidadContext contabilidadContext,
            ILogger<ImportarDteService> logger,
            dteContext dteContext)
        {
            _contabilidadContext = contabilidadContext;
            _logger = logger;
            _dteContext = dteContext;
        }

        // ─────────────────────────────────────────────────────────
        // PASO 1: Validar sin guardar (preview)
        // Totalmente secuencial — rápido porque no llama a Hacienda
        // ─────────────────────────────────────────────────────────
        public async Task<ImportarDteMasivoResponse> ValidarDocumentos(
            ImportarDteMasivoRequest request)
        {
            var response = new ImportarDteMasivoResponse();
            var resultados = new List<DteImportResultItem>();

            foreach (var doc in request.Documentos)
            {
                var r = await ProcesarDocumento(doc, request.IdCliente, guardar: false);
                resultados.Add(r);
            }

            response.Resultados = resultados;
            response.TotalProcesados = resultados.Count;
            response.TotalExitosos = resultados.Count(r => r.Exitoso);
            response.TotalErrores = resultados.Count(r => !r.Exitoso);
            return response;
        }

        // ─────────────────────────────────────────────────────────
        // PASO 2: Validar + Guardar
        // ─────────────────────────────────────────────────────────
        public async Task<ImportarDteMasivoResponse> ValidarYGuardar(
            ConfirmarImportacionRequest request)
        {
            var response = new ImportarDteMasivoResponse();
            var proveedoresCreados = new List<string>();

            foreach (var item in request.Items)
            {
                var docItem = new DteJsonItem
                {
                    NombreArchivo = item.NombreArchivo,
                    ContenidoJson = item.ContenidoJson,
                    IdTipoCostoGastoOverride = item.IdTipoCostoGasto
                };

                var resultado = await ProcesarDocumento(
                    docItem,
                    request.IdCliente,
                    guardar: true,
                    idTipoOperacion: item.IdTipoOperacion,
                    idClasificacion: item.IdClasificacion,
                    idSector: item.IdSector);

                response.Resultados.Add(resultado);

                if (resultado.ProveedorNuevo && !string.IsNullOrEmpty(resultado.NombreEmisor))
                    proveedoresCreados.Add(resultado.NombreEmisor);
            }

            response.TotalProcesados = response.Resultados.Count;
            response.TotalExitosos = response.Resultados.Count(r => r.Exitoso);
            response.TotalErrores = response.Resultados.Count(r => !r.Exitoso);
            response.ProveedoresCreados = proveedoresCreados.Count;
            response.NombresProveedoresCreados = proveedoresCreados.Distinct().ToList();
            return response;
        }

        // ─────────────────────────────────────────────────────────
        // CORE: procesar un documento
        // Validación basada en selloRecibido del propio JSON
        // ─────────────────────────────────────────────────────────
        private async Task<DteImportResultItem> ProcesarDocumento(
            DteJsonItem doc,
            int idCliente,
            bool guardar,
            int idTipoOperacion = DEFAULT_TIPO_OPERACION,
            int idClasificacion = DEFAULT_CLASIFICACION,
            int idSector = DEFAULT_SECTOR)
        {
            var result = new DteImportResultItem { NombreArchivo = doc.NombreArchivo };

            try
            {
                // ── 1. PARSEAR JSON ──────────────────────────────
                JsonDocument jsonDoc;
                try { jsonDoc = JsonDocument.Parse(doc.ContenidoJson); }
                catch (JsonException ex)
                {
                    result.Estado = "ERROR_PARSEO";
                    result.MensajeError = $"JSON inválido: {ex.Message}";
                    return result;
                }

                var root = jsonDoc.RootElement;

                // ── 2. IDENTIFICACIÓN ────────────────────────────
                var identificacion = GetElement(root, "identificacion");
                if (identificacion == null)
                {
                    result.Estado = "ERROR_PARSEO";
                    result.MensajeError = "El JSON no contiene el nodo 'identificacion'";
                    return result;
                }

                var tipoDte = GetString(identificacion.Value, "tipoDte") ?? "";
                var codigoGeneracion = GetString(identificacion.Value, "codigoGeneracion") ?? "";
                var fecEmi = GetString(identificacion.Value, "fecEmi") ?? "";

                result.TipoDte = tipoDte;
                result.CodigoGeneracion = codigoGeneracion;
                result.FechaEmision = fecEmi;

                // ── 3. VALIDAR TIPO DTE ──────────────────────────
                if (!TiposValidos.Contains(tipoDte))
                {
                    result.Estado = "INVALIDO_TIPO";
                    result.Exitoso = false;
                    result.MensajeError = tipoDte == "01"
                        ? "FCF (tipo 01) no corresponde al Libro de Compras."
                        : $"Tipo '{tipoDte}' no válido. Aceptados: 03=CCF, 05=NC, 06=ND.";
                    return result;
                }

                // ── 4. VERIFICAR SELLO EN EL JSON ────────────────
                // Distintos proveedores guardan el sello en distintos lugares/nombres:
                //   a) root.selloRecibido            — formato ContabSV propio
                //   b) root.responseMH.selloRecibido — formato BANNER y otros
                //   c) root.SelloRecepcion           — formato COPLASA y otros
                //   d) root.responseMH.SelloRecepcion — variante mixta
                var selloRecibido = GetString(root, "selloRecibido")
                                 ?? GetString(root, "SelloRecepcion");

                if (string.IsNullOrWhiteSpace(selloRecibido))
                {
                    var responseMH = GetElement(root, "responseMH");
                    if (responseMH.HasValue)
                        selloRecibido = GetString(responseMH.Value, "selloRecibido")
                                     ?? GetString(responseMH.Value, "SelloRecepcion");
                }

                bool tieneSello = !string.IsNullOrWhiteSpace(selloRecibido);

                if (tieneSello)
                {
                    result.Estado = "VALIDO";
                    result.Exitoso = true;
                    result.EstadoHacienda = "PROCESADO";
                }
                else
                {
                    result.Estado = "SIN_SELLO";
                    result.Exitoso = true;   // permitir importar — usuario decide
                    result.EstadoHacienda = "SIN_VERIFICAR";
                    result.MensajeError = "El JSON no incluye sello de Hacienda. Puede importarlo pero verifique manualmente.";
                }

                // ── 5. EMISOR ────────────────────────────────────
                var emisor = GetElement(root, "emisor");
                result.NombreEmisor = emisor.HasValue
                    ? (GetString(emisor.Value, "nombre") ?? GetString(emisor.Value, "nombreComercial") ?? "")
                    : "";
                result.NitEmisor = emisor.HasValue ? GetString(emisor.Value, "nit") ?? "" : "";

                // ── 6. MONTOS ────────────────────────────────────
                var resumen = GetElement(root, "resumen") ?? GetElement(root, "summary");
                decimal totalPagar = 0, ivaTotal = 0, subTotal = 0;
                if (resumen.HasValue)
                {
                    totalPagar = GetDecimal(resumen.Value, "totalPagar") ?? GetDecimal(resumen.Value, "total_to_pay") ?? 0;
                    ivaTotal = GetDecimal(resumen.Value, "totalIva") ?? GetDecimal(resumen.Value, "total_iva") ?? 0;
                    subTotal = GetDecimal(resumen.Value, "subTotalVentas") ?? GetDecimal(resumen.Value, "sub_total_sales") ?? 0;
                }
                decimal totalCompras = subTotal > 0 ? subTotal : (totalPagar - ivaTotal);
                decimal creditoFiscal = ivaTotal > 0 ? ivaTotal : Math.Round(totalCompras * 0.13m, 2);
                result.TotalCompra = totalCompras;
                result.CreditoFiscal = creditoFiscal;

                // ── 7. VERIFICAR DUPLICADO ───────────────────────
                if (!string.IsNullOrEmpty(codigoGeneracion))
                {
                    var yaExiste = await _contabilidadContext.Compras
                        .AnyAsync(c => c.NumeroDocumento == codigoGeneracion
                                    && c.IdCliente == idCliente
                                    && c.Eliminado == false);
                    if (yaExiste)
                    {
                        result.Estado = "YA_REGISTRADO";
                        result.Exitoso = false;
                        result.MensajeError = "Este documento ya existe en el Libro de Compras.";
                        return result;
                    }
                }

                // ── 8. SI ES SOLO PREVIEW, TERMINAR AQUÍ ────────
                if (!guardar)
                {
                    // Informar si el proveedor ya existe (sin crearlo)
                    var nit = result.NitEmisor;
                    if (!string.IsNullOrEmpty(nit))
                    {
                        var nrc = emisor.HasValue ? GetString(emisor.Value, "nrc") ?? "" : "";
                        result.ProveedorNuevo = !await _contabilidadContext.Proveedores
                            .AnyAsync(p => p.IdCliente == idCliente
                                        && (p.NitProveedor == nit || p.Nrc == nrc));
                    }
                    return result;
                }

                // ── 9. BUSCAR O CREAR PROVEEDOR ──────────────────
                int idProveedor = 0;
                bool provNuevo = false;
                var nitEmisor = result.NitEmisor ?? "";
                var nrcEmisor = emisor.HasValue ? GetString(emisor.Value, "nrc") ?? "" : "";
                var nombreEmisor = result.NombreEmisor ?? "";

                if (!string.IsNullOrEmpty(nitEmisor))
                {
                    var provExistente = await _contabilidadContext.Proveedores
                        .Where(p => p.IdCliente == idCliente
                                 && (p.NitProveedor == nitEmisor || p.Nrc == nrcEmisor))
                        .FirstOrDefaultAsync();

                    if (provExistente != null)
                    {
                        idProveedor = provExistente.IdProveedor;
                    }
                    else
                    {
                        bool esJuridico = nitEmisor.Length == 14;
                        var nuevoProv = new Proveedore
                        {
                            IdCliente = idCliente,
                            NitProveedor = nitEmisor,
                            Nrc = nrcEmisor,
                            PersonaJuridica = esJuridico,
                            NombreRazonSocial = esJuridico ? nombreEmisor : "",
                            Nombres = esJuridico ? "" : nombreEmisor,
                            Apellidos = "",
                            NombreComercial = emisor.HasValue ? GetString(emisor.Value, "nombreComercial") ?? nombreEmisor : nombreEmisor,
                            TelefonoCliente = emisor.HasValue ? GetString(emisor.Value, "telefono") ?? "" : "",
                            Email = emisor.HasValue ? GetString(emisor.Value, "correo") ?? "" : "",
                            Direccion = "",
                            IdSector = idSector > 0 ? idSector : DEFAULT_SECTOR,
                            TipoContribuyente = emisor.HasValue ? GetString(emisor.Value, "tipoContribuyente") ?? "OTRO" : "OTRO",
                        };
                        _contabilidadContext.Proveedores.Add(nuevoProv);
                        await _contabilidadContext.SaveChangesAsync();
                        idProveedor = nuevoProv.IdProveedor;
                        provNuevo = true;
                        result.ProveedorNuevo = true;
                    }
                }

                result.IdProveedorAsignado = idProveedor;

                // ── 10. CREAR REGISTRO EN LIBRO DE COMPRAS ───────
                var nuevaCompra = new Compra
                {
                    FechaCreacion = DateTime.Now,
                    FechaEmision = DateOnly.TryParse(fecEmi, out var fe)
                                        ? fe : DateOnly.FromDateTime(DateTime.Now),
                    FechaPresentacion = DateOnly.FromDateTime(DateTime.Now),
                    IdclaseDocumento = DEFAULT_CLASE_DOCUMENTO,
                    IdtipoDocumento = tipoDte switch { "03" => 3, "05" => 5, "06" => 6, _ => DEFAULT_TIPO_DOCUMENTO },
                    NumeroDocumento = codigoGeneracion,
                    InternasGravadas = totalCompras,
                    CreditoFiscal = creditoFiscal,
                    TotalCompras = totalCompras,
                    InternasExentas = 0,
                    InternacionalesExentasYONsujetas = 0,
                    ImportacionesYONsujetas = 0,
                    InternacionesGravadasBienes = 0,
                    ImportacionesGravadasBienes = 0,
                    ImportacionesGravadasServicios = 0,
                    IdTipoOperacion = idTipoOperacion > 0 ? idTipoOperacion : DEFAULT_TIPO_OPERACION,
                    IdClasificacion = idClasificacion > 0 ? idClasificacion : DEFAULT_CLASIFICACION,
                    IdTipoCostoGasto = doc.IdTipoCostoGastoOverride ?? DEFAULT_TIPO_COSTO_GASTO,
                    IdSector = idSector > 0 ? idSector : DEFAULT_SECTOR,
                    NumeroAnexo = "3",
                    Posteado = false,
                    Anulado = false,
                    Eliminado = false,
                    IdCliente = idCliente,
                    Combustible = false,
                    NumSerie = null,
                    IvaRetenido = 0,
                    IdProveedor = idProveedor
                };

                _contabilidadContext.Compras.Add(nuevaCompra);
                await _contabilidadContext.SaveChangesAsync();

                result.IdCompraCreada = nuevaCompra.IdDocCompra;
                result.Exitoso = true;
                result.ProveedorNuevo = provNuevo;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando {NombreArchivo}", doc.NombreArchivo);
                result.Estado = "ERROR_INTERNO";
                result.Exitoso = false;
                result.MensajeError = $"Error interno: {ex.Message}";
                return result;
            }
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────
        private static JsonElement? GetElement(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out var el)) return el;
            var pascal = char.ToUpper(key[0]) + key[1..];
            if (root.TryGetProperty(pascal, out var el2)) return el2;
            return null;
        }

        private static string? GetString(JsonElement el, string key)
        {
            var prop = GetElement(el, key);
            return prop?.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop?.ToString();
        }

        private static decimal? GetDecimal(JsonElement el, string key)
        {
            var prop = GetElement(el, key);
            if (prop == null) return null;
            if (prop.Value.ValueKind == JsonValueKind.Number) return prop.Value.GetDecimal();
            if (decimal.TryParse(prop.Value.ToString(), out var d)) return d;
            return null;
        }
    }
}