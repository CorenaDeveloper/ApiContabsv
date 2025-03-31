using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    public class Consumidor_FinalDTO
    {
        [SwaggerSchema(Description = "ID de la venta consumidor")]
        public int IdVentaConsumidor { get; set; }

        [SwaggerSchema(Description = "Fecha de presentación")]
        public DateOnly FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión")]
        public DateOnly FechaEmision { get; set; }

        [SwaggerSchema(Description = "ID de la clase de documento")]
        public int IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "ID del tipo de documento")]
        public int IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número de resolución")]
        public string NumeroResolucion { get; set; }
        
        [SwaggerSchema(Description = "Número de documento emitido")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Serie del documento")]
        public string SerieDocumento { get; set; }

        [SwaggerSchema(Description = "Número de control interno")]
        public string? NumeroControlInterno { get; set; }

        [SwaggerSchema(Description = "Número de la máquina registradora")]
        public string? NumeroMaquinaRegistradora { get; set; }

        [SwaggerSchema(Description = "Ventas exentas")]
        public decimal? VentasExentas { get; set; }

        [SwaggerSchema(Description = "Ventas internas exentas sin proporcionalidad")]
        public decimal? VentasInternasExentasNoProporcionalidad { get; set; }

        [SwaggerSchema(Description = "Ventas no sujetas")]
        public decimal? VentasNoSujetas { get; set; }

        [SwaggerSchema(Description = "Ventas gravadas locales")]
        public decimal? VentasGravadasLocales { get; set; }

        [SwaggerSchema(Description = "Exportaciones a Centroamérica")]
        public decimal? ExportacionesCentroamerica { get; set; }

        [SwaggerSchema(Description = "Exportaciones fuera de Centroamérica")]
        public decimal? ExportacionesFueraCentroamerica { get; set; }

        [SwaggerSchema(Description = "Exportaciones de servicio")]
        public decimal? ExportacionesServicio { get; set; }

        [SwaggerSchema(Description = "Ventas a zonas francas DPA")]
        public decimal? VentasZonasFrancasDpa { get; set; }

        [SwaggerSchema(Description = "Ventas a terceros no domiciliados")]
        public decimal? VentasTercerosNoDomiciliados { get; set; }

        [SwaggerSchema(Description = "Total de ventas")]
        public decimal? TotalVentas { get; set; }

        [SwaggerSchema(Description = "ID del tipo de operación CG")]
        public int IdTipoOperacionCg { get; set; }

        [SwaggerSchema(Description = "ID de la operación")]
        public int IdOperacion { get; set; }

        [SwaggerSchema(Description = "Número de anexo")]
        public int NumeroAnexo { get; set; }

        [SwaggerSchema(Description = "ID del cliente")]
        public int IdCliente { get; set; }

        [SwaggerSchema(Description = "ID del cliente CIT")]
        public int? IdClienteCit { get; set; }
    }

    public class ListConsumidor_FinalDTO
    {
        [SwaggerSchema(Description = "ID de la venta consumidor")]
        public int IdVentaConsumidor { get; set; }

        [SwaggerSchema(Description = "Fecha de creación")]
        public DateOnly? FechaCreacion { get; set; }

        [SwaggerSchema(Description = "Fecha de presentación")]
        public DateOnly? FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión")]
        public DateOnly? FechaEmision { get; set; }

        [SwaggerSchema(Description = "ID de la clase de documento")]
        public int? IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "ID del tipo de documento")]
        public int? IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número de resolución")]
        public string NumeroResolucion { get; set; }

        [SwaggerSchema(Description = "Número de documento emitido")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Serie del documento")]
        public string SerieDocumento { get; set; }

        [SwaggerSchema(Description = "Número de la máquina registradora")]
        public string NumeroMaquinaRegistradora { get; set; }

        [SwaggerSchema(Description = "Ventas exentas")]
        public decimal? VentasExentas { get; set; }

        [SwaggerSchema(Description = "Ventas internas exentas sin proporcionalidad")]
        public decimal? VentasInternasExentasNoProporcionalidad { get; set; }

        [SwaggerSchema(Description = "Ventas no sujetas")]
        public decimal? VentasNoSujetas { get; set; }

        [SwaggerSchema(Description = "Ventas gravadas locales")]
        public decimal? VentasGravadasLocales { get; set; }

        [SwaggerSchema(Description = "Exportaciones a Centroamérica")]
        public decimal? ExportacionesCentroamerica { get; set; }

        [SwaggerSchema(Description = "Exportaciones fuera de Centroamérica")]
        public decimal? ExportacionesFueraCentroamerica { get; set; }

        [SwaggerSchema(Description = "Exportaciones de servicio")]
        public decimal? ExportacionesServicio { get; set; }

        [SwaggerSchema(Description = "Ventas a zonas francas DPA")]
        public decimal? VentasZonasFrancasDpa { get; set; }

        [SwaggerSchema(Description = "Ventas a terceros no domiciliados")]
        public decimal? VentasTercerosNoDomiciliados { get; set; }

        [SwaggerSchema(Description = "Total de ventas")]
        public decimal? TotalVentas { get; set; }

        [SwaggerSchema(Description = "ID del tipo de operación CG")]
        public int? IdTipoOperacionCg { get; set; }

        [SwaggerSchema(Description = "ID de la operación")]
        public int? IdOperacion { get; set; }

        [SwaggerSchema(Description = "Número de anexo")]
        public int? NumeroAnexo { get; set; }

        [SwaggerSchema(Description = "Indica si está posteado")]
        public bool? Posteado { get; set; }

        [SwaggerSchema(Description = "Indica si está anulado")]
        public bool? Anulado { get; set; }

        [SwaggerSchema(Description = "Indica si está eliminado")]
        public bool? Eliminado { get; set; }

        [SwaggerSchema(Description = "ID del cliente")]
        public int? IdCliente { get; set; }

        [SwaggerSchema(Description = "ID del cliente CIT")]
        public int? IdClienteCit { get; set; }
    }
}
