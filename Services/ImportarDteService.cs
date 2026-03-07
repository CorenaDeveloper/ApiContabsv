
using ApiContabsv.DTO.DB_ContabilidadDTO;
using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Dte;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiContabsv.Services
{
    public interface IImportarDteService
    {
        Task<ImportarDteMasivoResponse> ValidarDocumentos(
            ImportarDteMasivoRequest request);

        Task<ImportarDteMasivoResponse> ValidarYGuardar(
            ConfirmarImportacionRequest request);
    }

    public class ImportarDteService : IImportarDteService
    {
        private readonly ContabilidadContext _contabilidadContext;
        private readonly IHaciendaService _haciendaService;
        private readonly ILogger<ImportarDteService> _logger;
        private readonly dteContext _dteContext;

        // Tipos DTE válidos para Libro de Compras (Anexo 3)
        private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "03", // CCF - Comprobante de Crédito Fiscal
            "05", // NC  - Nota de Crédito
            "06"  // ND  - Nota de Débito
        };

        // Valores por defecto para campos de clasificación
        // Ajusta estos IDs según tu base de datos
        private const int DEFAULT_TIPO_OPERACION = 1;
        private const int DEFAULT_CLASIFICACION = 1;
        private const int DEFAULT_TIPO_COSTO_GASTO = 1;
        private const int DEFAULT_SECTOR = 1;
        private const int DEFAULT_CLASE_DOCUMENTO = 1; 
        private const int DEFAULT_TIPO_DOCUMENTO = 3; 

        public ImportarDteService(
            ContabilidadContext contabilidadContext,
            IHaciendaService haciendaService,
            ILogger<ImportarDteService> logger,
            dteContext dteContext)
        {
            _contabilidadContext = contabilidadContext;
            _haciendaService = haciendaService;
            _logger = logger;
            _dteContext = dteContext;
        }

        // ─────────────────────────────────────────────────────────
        // PASO 1: Solo validar (sin guardar) — para el preview
        // ─────────────────────────────────────────────────────────
        public async Task<ImportarDteMasivoResponse> ValidarDocumentos(
            ImportarDteMasivoRequest request)
        {
            var response = new ImportarDteMasivoResponse();

            // Obtener usuario para consultas a Hacienda
            var user = await _dteContext.Users.FindAsync(request.UserId);

            // Procesar cada documento en paralelo (máx 5 simultáneos para no saturar Hacienda)
            var semaphore = new SemaphoreSlim(5);
            var tasks = request.Documentos.Select(async doc =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ProcesarDocumento(doc, request.IdCliente, request.Ambiente, user, guardar: false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var resultados = await Task.WhenAll(tasks);

            response.Resultados = resultados.ToList();
            response.TotalProcesados = resultados.Length;
            response.TotalExitosos = resultados.Count(r => r.Exitoso);
            response.TotalErrores = resultados.Count(r => !r.Exitoso);

            return response;
        }

        // ─────────────────────────────────────────────────────────
        // PASO 2: Validar + Guardar definitivamente
        // ─────────────────────────────────────────────────────────
        public async Task<ImportarDteMasivoResponse> ValidarYGuardar(
            ConfirmarImportacionRequest request)
        {
            var response = new ImportarDteMasivoResponse();
            var proveedoresCreados = new List<string>();

            var user = await _dteContext.Users.FindAsync(request.UserId);

            // Procesar secuencialmente para evitar duplicados de proveedores
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
                    request.Ambiente,
                    user,
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
        // CORE: Procesar un documento individual
        // ─────────────────────────────────────────────────────────
        private async Task<DteImportResultItem> ProcesarDocumento(
            DteJsonItem doc,
            int idCliente,
            string ambiente,
            dynamic? user,
            bool guardar,
            int idTipoOperacion = DEFAULT_TIPO_OPERACION,
            int idClasificacion = DEFAULT_CLASIFICACION,
            int idSector = DEFAULT_SECTOR)
        {
            var result = new DteImportResultItem
            {
                NombreArchivo = doc.NombreArchivo
            };

            try
            {
                // ── 1. PARSEAR JSON ──────────────────────────────
                JsonDocument jsonDoc;
                try
                {
                    jsonDoc = JsonDocument.Parse(doc.ContenidoJson);
                }
                catch (JsonException ex)
                {
                    result.Estado = "ERROR_PARSEO";
                    result.MensajeError = $"JSON inválido: {ex.Message}";
                    return result;
                }

                var root = jsonDoc.RootElement;

                // Extraer identificación
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

                // ── 2. VALIDAR TIPO DTE ──────────────────────────
                if (!TiposValidos.Contains(tipoDte))
                {
                    result.Estado = "INVALIDO_TIPO";
                    result.Exitoso = false;
                    result.MensajeError = tipoDte == "01"
                        ? "Las Facturas de Consumidor Final (01) no generan crédito fiscal y no deben incluirse en el Libro de Compras."
                        : $"Tipo DTE '{tipoDte}' no es válido para el Libro de Compras. Tipos válidos: 03=CCF, 05=NC, 06=ND.";
                    return result;
                }

                // ── 3. VERIFICAR SELLO ───────────────────────────
                var selloRecibido = GetString(root, "selloRecibido");
                if (string.IsNullOrWhiteSpace(selloRecibido))
                {
                    result.Estado = "SIN_SELLO";
                    result.Exitoso = false;
                    result.MensajeError = "El documento no tiene sello de recibido de Hacienda. Puede que no haya sido transmitido correctamente.";
                    // No bloqueamos: dejamos que el usuario decida
                }

                // ── 4. EXTRAER DATOS DEL EMISOR ──────────────────
                var emisor = GetElement(root, "emisor");
                var nit = emisor.HasValue ? GetString(emisor.Value, "nit") ?? "" : "";
                var nrc = emisor.HasValue ? GetString(emisor.Value, "nrc") ?? "" : "";
                var nombre = emisor.HasValue
                    ? (GetString(emisor.Value, "nombre") ?? GetString(emisor.Value, "nombreComercial") ?? "")
                    : "";

                result.NombreEmisor = nombre;
                result.NitEmisor = nit;

                // ── 5. EXTRAER MONTOS DEL RESUMEN ────────────────
                var resumen = GetElement(root, "resumen") ?? GetElement(root, "summary");
                decimal totalPagar = 0, ivaTotal = 0, subTotal = 0;

                if (resumen.HasValue)
                {
                    totalPagar = GetDecimal(resumen.Value, "totalPagar")
                              ?? GetDecimal(resumen.Value, "total_to_pay") ?? 0;
                    ivaTotal = GetDecimal(resumen.Value, "totalIva")
                              ?? GetDecimal(resumen.Value, "total_iva") ?? 0;
                    subTotal = GetDecimal(resumen.Value, "subTotalVentas")
                              ?? GetDecimal(resumen.Value, "sub_total_sales") ?? 0;
                }

                // Para CCF: totalCompras = subtotal (sin IVA), creditoFiscal = IVA
                decimal totalCompras = subTotal > 0 ? subTotal : (totalPagar - ivaTotal);
                decimal creditoFiscal = ivaTotal > 0 ? ivaTotal : Math.Round(totalCompras * 0.13m, 2);

                result.TotalCompra = totalCompras;
                result.CreditoFiscal = creditoFiscal;

                // ── 6. VERIFICAR DUPLICADO ───────────────────────
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

                // ── 7. CONSULTAR HACIENDA ────────────────────────
                if (user != null && !string.IsNullOrEmpty(codigoGeneracion))
                {
                    try
                    {
                        var consultaResult = await _haciendaService.ConsultarDTE(
                            (string)user.Nit,
                            codigoGeneracion,
                            ambiente,
                            tipoDte);

                        if (consultaResult.HasValue)
                        {
                            var estadoH = GetString(consultaResult.Value, "estado")
                                       ?? GetString(consultaResult.Value, "Estado")
                                       ?? "";
                            result.EstadoHacienda = estadoH.ToUpper();

                            if (result.EstadoHacienda == "RECHAZADO")
                            {
                                result.Estado = "RECHAZADO_HACIENDA";
                                result.Exitoso = false;
                                result.MensajeError = "Hacienda reporta este documento como RECHAZADO.";
                                // No bloqueamos — el frontend decidirá mostrarlo en rojo
                            }
                            else if (result.EstadoHacienda == "PROCESADO")
                            {
                                result.Estado = "VALIDO";
                                result.Exitoso = true;
                            }
                            else
                            {
                                // Estado desconocido pero sin rechazo explícito
                                result.Estado = string.IsNullOrEmpty(result.Estado) ? "VALIDO" : result.Estado;
                                result.Exitoso = string.IsNullOrEmpty(result.MensajeError);
                            }
                        }
                        else
                        {
                            // No se pudo consultar Hacienda — marcamos como válido con advertencia
                            result.Estado = string.IsNullOrEmpty(result.Estado) ? "VALIDO" : result.Estado;
                            result.EstadoHacienda = "NO_CONSULTADO";
                            result.Exitoso = string.IsNullOrEmpty(result.MensajeError);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo consultar Hacienda para {CodGen}", codigoGeneracion);
                        result.EstadoHacienda = "ERROR_CONSULTA";
                        result.Estado = string.IsNullOrEmpty(result.Estado) ? "VALIDO" : result.Estado;
                        result.Exitoso = string.IsNullOrEmpty(result.MensajeError);
                    }
                }
                else
                {
                    result.Estado = string.IsNullOrEmpty(result.Estado) ? "VALIDO" : result.Estado;
                    result.Exitoso = string.IsNullOrEmpty(result.MensajeError);
                }

                // ── 8. SI NO GUARDAR, TERMINAR AQUÍ ─────────────
                if (!guardar)
                    return result;

                // ── 9. BUSCAR O CREAR PROVEEDOR ──────────────────
                int idProveedor = 0;
                bool provNuevo = false;

                if (!string.IsNullOrEmpty(nit))
                {
                    var provExistente = await _contabilidadContext.Proveedores
                        .Where(p => p.IdCliente == idCliente
                                 && (p.NitProveedor == nit || p.Nrc == nrc))
                        .FirstOrDefaultAsync();

                    if (provExistente != null)
                    {
                        idProveedor = provExistente.IdProveedor;
                    }
                    else
                    {
                        // Crear proveedor automáticamente con datos del JSON
                        bool esJuridico = nit.Length == 14; // NITs de persona jurídica tienen 14 chars
                        var nuevoProv = new Proveedore
                        {
                            IdCliente = idCliente,
                            NitProveedor = nit,
                            Nrc = nrc,
                            PersonaJuridica = esJuridico,
                            NombreRazonSocial = esJuridico ? nombre : "",
                            Nombres = esJuridico ? "" : nombre,
                            Apellidos = "",
                            NombreComercial = GetString(emisor!.Value, "nombreComercial") ?? nombre,
                            TelefonoCliente = GetString(emisor!.Value, "telefono") ?? "",
                            Email = GetString(emisor!.Value, "correo") ?? "",
                            Direccion = "",
                            IdSector = idSector > 0 ? idSector : DEFAULT_SECTOR,
                            TipoContribuyente = GetString(emisor!.Value, "tipoContribuyente") ?? "OTRO",
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
                // Solo guardar si es válido o si el usuario forzó el guardado
                if (result.Exitoso || result.Estado == "SIN_SELLO")
                {
                    var nuevaCompra = new Compra
                    {
                        FechaCreacion = DateTime.Now,
                        FechaEmision = DateOnly.TryParse(fecEmi, out var fe) ? fe : DateOnly.FromDateTime(DateTime.Now),
                        FechaPresentacion = DateOnly.FromDateTime(DateTime.Now),
                        IdclaseDocumento = DEFAULT_CLASE_DOCUMENTO,
                        IdtipoDocumento = tipoDte switch
                        {
                            "03" => 3,
                            "05" => 5,
                            "06" => 6,
                            _ => DEFAULT_TIPO_DOCUMENTO
                        },
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
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando documento {NombreArchivo}", doc.NombreArchivo);
                result.Estado = "ERROR_INTERNO";
                result.Exitoso = false;
                result.MensajeError = $"Error interno: {ex.Message}";
                return result;
            }
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS para extraer valores del JSON
        // ─────────────────────────────────────────────────────────
        private static JsonElement? GetElement(JsonElement root, string key)
        {
            if (root.TryGetProperty(key, out var el))
                return el;
            // Intentar camelCase y PascalCase
            var pascal = char.ToUpper(key[0]) + key[1..];
            if (root.TryGetProperty(pascal, out var el2))
                return el2;
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
            if (prop.Value.ValueKind == JsonValueKind.Number)
                return prop.Value.GetDecimal();
            if (decimal.TryParse(prop.Value.ToString(), out var d))
                return d;
            return null;
        }
    }
}