using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.DTO.DB_ContabilidadDTO
{

    public class ListVentaContribuyenteDTO
    {

        [SwaggerSchema(Description = "Fecha de presentación del documento")]
        public DateOnly? FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión del documento")]
        public DateOnly? FechaEmisionDocumento { get; set; }

        [SwaggerSchema(Description = "Identificador de la clase de documento")]
        public int? IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "Identificador del tipo de documento")]
        public int? IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número de resolución del documento")]
        public string NumeroResolucion { get; set; }

        [SwaggerSchema(Description = "Serie del documento")]
        public string SerieDocumento { get; set; }

        [SwaggerSchema(Description = "Número del documento")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Número de control interno")]
        public string NumeroControlInterno { get; set; }

        [SwaggerSchema(Description = "Monto de ventas exentas")]
        public decimal? VentasExentas { get; set; }

        [SwaggerSchema(Description = "Monto de ventas no sujetas")]
        public decimal? VentasNoSujetas { get; set; }

        [SwaggerSchema(Description = "Monto de ventas gravadas locales")]
        public decimal? VentasGravadasLocales { get; set; }

        [SwaggerSchema(Description = "Débito fiscal aplicado")]
        public decimal? DebitoFiscal { get; set; }

        [SwaggerSchema(Description = "Ventas a terceros no domiciliados")]
        public decimal? VentasTercerosNoDomiciliados { get; set; }

        [SwaggerSchema(Description = "Débito fiscal en ventas a terceros")]
        public decimal? DebitoFiscalVentasTerceros { get; set; }

        [SwaggerSchema(Description = "Monto total de ventas")]
        public decimal? TotalVentas { get; set; }

        [SwaggerSchema(Description = "Identificador del tipo de operación contable")]
        public int? IdTipoOperacionCg { get; set; }

        [SwaggerSchema(Description = "Identificador de la operación realizada")]
        public int? IdOperacion { get; set; }

        [SwaggerSchema(Description = "Identificador del cliente")]
        public int? IdCliente { get; set; }

        [SwaggerSchema(Description = "Identificador del cliente CIT")]
        public int? IdClienteCit { get; set; }
    }
    public class VentaContribuyenteDTO
    {

        [SwaggerSchema(Description = "ID de ventra a contribuyente")]
        public int? IdVentaContribuyentes { get; set; }
        
        [SwaggerSchema(Description = "Fecha de presentación del documento")]
        public DateOnly? FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión del documento")]
        public DateOnly? FechaEmisionDocumento { get; set; }

        [SwaggerSchema(Description = "Identificador de la clase de documento")]
        public int? IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "Identificador del tipo de documento")]
        public int? IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número de resolución del documento")]
        public string NumeroResolucion { get; set; }

        [SwaggerSchema(Description = "Serie del documento")]
        public string SerieDocumento { get; set; }

        [SwaggerSchema(Description = "Número del documento")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Número de control interno")]
        public string NumeroControlInterno { get; set; }

        [SwaggerSchema(Description = "Monto de ventas exentas")]
        public decimal? VentasExentas { get; set; }

        [SwaggerSchema(Description = "Monto de ventas no sujetas")]
        public decimal? VentasNoSujetas { get; set; }

        [SwaggerSchema(Description = "Monto de ventas gravadas locales")]
        public decimal? VentasGravadasLocales { get; set; }

        [SwaggerSchema(Description = "Débito fiscal aplicado")]
        public decimal? DebitoFiscal { get; set; }

        [SwaggerSchema(Description = "Ventas a terceros no domiciliados")]
        public decimal? VentasTercerosNoDomiciliados { get; set; }

        [SwaggerSchema(Description = "Débito fiscal en ventas a terceros")]
        public decimal? DebitoFiscalVentasTerceros { get; set; }

        [SwaggerSchema(Description = "Monto total de ventas")]
        public decimal? TotalVentas { get; set; }

        [SwaggerSchema(Description = "Identificador del tipo de operación contable")]
        public int? IdTipoOperacionCg { get; set; }

        [SwaggerSchema(Description = "Identificador de la operación realizada")]
        public int? IdOperacion { get; set; }

        [SwaggerSchema(Description = "Identificador del cliente")]
        public int? IdCliente { get; set; }

        [SwaggerSchema(Description = "Identificador del cliente CIT")]
        public int? IdClienteCit { get; set; }
    }
}
