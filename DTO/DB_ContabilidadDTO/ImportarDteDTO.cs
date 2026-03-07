
using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    /// <summary>
    /// Request principal: lista de JSONs DTE + configuración del cliente
    /// </summary>
    public class ImportarDteMasivoRequest
    {
        [SwaggerSchema(Description = "ID del cliente/empresa que está importando")]
        public int IdCliente { get; set; }

        [SwaggerSchema(Description = "ID del usuario autenticado (para consultar Hacienda)")]
        public int UserId { get; set; }

        [SwaggerSchema(Description = "Ambiente: 00=pruebas, 01=producción")]
        public string Ambiente { get; set; } = "01";

        [SwaggerSchema(Description = "Lista de DTEs a procesar. Cada item es el contenido JSON del archivo")]
        public List<DteJsonItem> Documentos { get; set; } = new();
    }

    /// <summary>
    /// Un documento DTE individual (contenido del .json)
    /// </summary>
    public class DteJsonItem
    {
        [SwaggerSchema(Description = "Nombre del archivo original (ej: credito-fiscal-001.json)")]
        public string NombreArchivo { get; set; } = "";

        [SwaggerSchema(Description = "Contenido completo del JSON como string")]
        public string ContenidoJson { get; set; } = "";

        [SwaggerSchema(Description = "Override del IdTipoCostoGasto (si el usuario lo cambia antes de guardar)")]
        public int? IdTipoCostoGastoOverride { get; set; }
    }

    // ============================================================
    // RESPONSE
    // ============================================================

    /// <summary>
    /// Resultado completo de la importación masiva
    /// </summary>
    public class ImportarDteMasivoResponse
    {
        public int TotalProcesados { get; set; }
        public int TotalExitosos { get; set; }
        public int TotalErrores { get; set; }
        public int ProveedoresCreados { get; set; }
        public List<string> NombresProveedoresCreados { get; set; } = new();
        public List<DteImportResultItem> Resultados { get; set; } = new();
    }

    /// <summary>
    /// Resultado de procesar un DTE individual
    /// </summary>
    public class DteImportResultItem
    {
        public string NombreArchivo { get; set; } = "";
        public string CodigoGeneracion { get; set; } = "";
        public string TipoDte { get; set; } = "";
        public string NombreEmisor { get; set; } = "";
        public string NitEmisor { get; set; } = "";
        public decimal? TotalCompra { get; set; }
        public decimal? CreditoFiscal { get; set; }

        /// <summary>
        /// VALIDO, INVALIDO_TIPO, SIN_SELLO, RECHAZADO_HACIENDA, PROCESADO_HACIENDA, YA_REGISTRADO, ERROR_PARSEO
        /// </summary>
        public string Estado { get; set; } = "";

        public bool Exitoso { get; set; }
        public string? MensajeError { get; set; }

        /// <summary>
        /// Estado devuelto por Hacienda: PROCESADO, RECHAZADO, etc.
        /// </summary>
        public string? EstadoHacienda { get; set; }

        public bool ProveedorNuevo { get; set; }
        public int? IdProveedorAsignado { get; set; }
        public int? IdCompraCreada { get; set; }
        public string? FechaEmision { get; set; }
    }

    /// <summary>
    /// Request para guardar definitivamente los DTEs ya validados
    /// Permite al usuario modificar el tipo de costo/gasto antes de guardar
    /// </summary>
    public class ConfirmarImportacionRequest
    {
        public int IdCliente { get; set; }
        public int UserId { get; set; }
        public string Ambiente { get; set; } = "01";
        public List<DteConfirmarItem> Items { get; set; } = new();
    }

    public class DteConfirmarItem
    {
        public string NombreArchivo { get; set; } = "";
        public string ContenidoJson { get; set; } = "";
        public int IdTipoCostoGasto { get; set; }
        public int IdTipoOperacion { get; set; }
        public int IdClasificacion { get; set; }
        public int IdSector { get; set; }
    }
}