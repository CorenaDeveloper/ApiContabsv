using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    public class ComprasDTO
    {
        [SwaggerSchema(Description = "Id de la compra")]
        public int IdDocCompra { get; set; }

        [SwaggerSchema(Description = "Fecha de creación del documento")]
        public DateTime FechaCreacion { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión del documento")]
        public DateTime FechaEmision { get; set; }

        [SwaggerSchema(Description = "Fecha de presentación del documento")]
        public DateTime FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "ID de la clase de documento")]
        public int IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "ID del tipo de documento")]
        public int IdTipoDocumento { get; set; }      
        
        [SwaggerSchema(Description = "Descripcion de tipo de documento")]
        public string DescripcionTipoDocumento { get; set; } 
        
        [SwaggerSchema(Description = "Codigo de tipo de documento")]
        public string CodigoTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número del documento")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Monto de internas exentas")]
        public decimal InternasExentas { get; set; }

        [SwaggerSchema(Description = "Monto de internacionales exentas y/o no sujetas")]
        public decimal InternacionalesExentasYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones y/o no sujetas")]
        public decimal ImportacionesYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de internas gravadas")]
        public decimal InternasGravadas { get; set; }

        [SwaggerSchema(Description = "Monto de internaciones gravadas (bienes)")]
        public decimal InternacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (bienes)")]
        public decimal ImportacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (servicios)")]
        public decimal ImportacionesGravadasServicios { get; set; }

        [SwaggerSchema(Description = "Crédito fiscal")]
        public decimal CreditoFiscal { get; set; }

        [SwaggerSchema(Description = "Total de compras")]
        public decimal TotalCompras { get; set; }

        [SwaggerSchema(Description = "ID del tipo de operación")]
        public int IdTipoOperacion { get; set; }

        [SwaggerSchema(Description = "ID de clasificación")]
        public int IdClasificacion { get; set; }

        [SwaggerSchema(Description = "ID del tipo de costo o gasto")]
        public int IdTipoCostoGasto { get; set; }

        [SwaggerSchema(Description = "ID del sector")]
        public int IdSector { get; set; }

        [SwaggerSchema(Description = "Número de anexo")]
        public string NumeroAnexo { get; set; }

        [SwaggerSchema(Description = "Indica si está posteado")]
        public bool Posteado { get; set; }

        [SwaggerSchema(Description = "Indica si está anulado")]
        public bool Anulado { get; set; }

        [SwaggerSchema(Description = "Indica si está eliminado")]
        public bool Eliminado { get; set; }

        [SwaggerSchema(Description = "ID del cliente")]
        public int IdCliente { get; set; }

        [SwaggerSchema(Description = "Indica si es combustible")]
        public bool Combustible { get; set; }

        [SwaggerSchema(Description = "Número de serie")]
        public string NumSerie { get; set; }

        [SwaggerSchema(Description = "IVA retenido")]
        public decimal IvaRetenido { get; set; }

        [SwaggerSchema(Description = "ID del proveedor")]
        public int IdProveedor { get; set; }

        // Información del proveedor
        [SwaggerSchema(Description = "Razón social del proveedor")]
        public string RazonProveedor { get; set; }

        [SwaggerSchema(Description = "Nombre del proveedor")]
        public string NombreProveedor { get; set; }

        [SwaggerSchema(Description = "Apellido del proveedor")]
        public string ApellidoProveedor { get; set; }        
        
        [SwaggerSchema(Description = "Nombre Comercial del Proveedor")]
        public string NombreComercial { get; set; }

        [SwaggerSchema(Description = "NIT del proveedor")]
        public string NitProveedor { get; set; }

        [SwaggerSchema(Description = "NRC del proveedor")]
        public string NRCProveedor { get; set; }

        [SwaggerSchema(Description = "Identifica si el proveedor es juridico")]
        public bool Juridico { get; set; }

        // Información de clasificación
        [SwaggerSchema(Description = "Código de clasificación")]
        public string CodigoClasificacion { get; set; }

        [SwaggerSchema(Description = "Descripción de la clasificación")]
        public string DescripcionClasificacion { get; set; }

        // Información de clase de documento
        [SwaggerSchema(Description = "Código de la clase de documento")]
        public string CodigoClaseDocumento { get; set; }

        [SwaggerSchema(Description = "Descripción de la clase de documento")]
        public string DescripcionClaseDocumento { get; set; }

        // Información del sector
        [SwaggerSchema(Description = "Código del sector")]
        public string CodigoSector { get; set; }

        [SwaggerSchema(Description = "Descripción del sector")]
        public string DescripcionSector { get; set; }

        // Información de operación
        [SwaggerSchema(Description = "Descripción del tipo de operación")]
        public string DescripcionTipOperacion { get; set; }

        [SwaggerSchema(Description = "Código de la operación")]
        public string CodigoOperacion { get; set; }

        [SwaggerSchema(Description = "Descripción de la operación")]
        public string DescripcionOperacion { get; set; }

        // Información de costo/gasto
        [SwaggerSchema(Description = "Código del tipo de costo o gasto")]
        public string CodigoTipoCostoGasto { get; set; }

        [SwaggerSchema(Description = "Descripción del costo o gasto")]
        public string DescripcionCostoGasto { get; set; }
    }

    public class CreateComprasDTO
    {

        [SwaggerSchema(Description = "Fecha de emisión del documento")]
        public DateOnly FechaEmision { get; set; }

        [SwaggerSchema(Description = "Fecha de presentación del documento")]
        public DateOnly FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "ID de la clase de documento")]
        public int IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "ID del tipo de documento")]
        public int IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número del documento")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Monto de internas exentas")]
        public decimal? InternasExentas { get; set; }

        [SwaggerSchema(Description = "Monto de internacionales exentas y/o no sujetas")]
        public decimal? InternacionalesExentasYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones y/o no sujetas")]
        public decimal? ImportacionesYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de internas gravadas")]
        public decimal? InternasGravadas { get; set; }

        [SwaggerSchema(Description = "Monto de internaciones gravadas (bienes)")]
        public decimal? InternacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (bienes)")]
        public decimal? ImportacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (servicios)")]
        public decimal? ImportacionesGravadasServicios { get; set; }

        [SwaggerSchema(Description = "Crédito fiscal")]
        public decimal? CreditoFiscal { get; set; }

        [SwaggerSchema(Description = "Total de compras")]
        public decimal? TotalCompras { get; set; }

        [SwaggerSchema(Description = "ID del tipo de operación")]
        public int IdTipoOperacion { get; set; }

        [SwaggerSchema(Description = "ID de clasificación")]
        public int IdClasificacion { get; set; }

        [SwaggerSchema(Description = "ID del tipo de costo o gasto")]
        public int IdTipoCostoGasto { get; set; }

        [SwaggerSchema(Description = "ID del sector")]
        public int IdSector { get; set; }

        [SwaggerSchema(Description = "Número de anexo")]
        public string NumeroAnexo { get; set; }

        [SwaggerSchema(Description = "ID del cliente")]
        public int IdCliente { get; set; }

        [SwaggerSchema(Description = "Indica si es combustible")]
        public bool Combustible { get; set; }

        [SwaggerSchema(Description = "Número de serie")]
        public string? NumSerie { get; set; }

        [SwaggerSchema(Description = "IVA retenido")]
        public decimal? IvaRetenido { get; set; }

        [SwaggerSchema(Description = "ID del proveedor")]
        public int IdProveedor { get; set; }

    }

    public class UpdateComprasDTO
    {
        [SwaggerSchema(Description = "Id de la compra")]
        public int IdDocCompra { get; set; }

        [SwaggerSchema(Description = "Fecha de emisión del documento")]
        public DateOnly FechaEmision { get; set; }

        [SwaggerSchema(Description = "Fecha de presentación del documento")]
        public DateOnly FechaPresentacion { get; set; }

        [SwaggerSchema(Description = "ID de la clase de documento")]
        public int IdClaseDocumento { get; set; }

        [SwaggerSchema(Description = "ID del tipo de documento")]
        public int IdTipoDocumento { get; set; }

        [SwaggerSchema(Description = "Número del documento")]
        public string NumeroDocumento { get; set; }

        [SwaggerSchema(Description = "Monto de internas exentas")]
        public decimal? InternasExentas { get; set; }

        [SwaggerSchema(Description = "Monto de internacionales exentas y/o no sujetas")]
        public decimal? InternacionalesExentasYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones y/o no sujetas")]
        public decimal? ImportacionesYONsujetas { get; set; }

        [SwaggerSchema(Description = "Monto de internas gravadas")]
        public decimal? InternasGravadas { get; set; }

        [SwaggerSchema(Description = "Monto de internaciones gravadas (bienes)")]
        public decimal? InternacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (bienes)")]
        public decimal? ImportacionesGravadasBienes { get; set; }

        [SwaggerSchema(Description = "Monto de importaciones gravadas (servicios)")]
        public decimal? ImportacionesGravadasServicios { get; set; }

        [SwaggerSchema(Description = "Crédito fiscal")]
        public decimal? CreditoFiscal { get; set; }

        [SwaggerSchema(Description = "Total de compras")]
        public decimal? TotalCompras { get; set; }

        [SwaggerSchema(Description = "ID del tipo de operación")]
        public int IdTipoOperacion { get; set; }

        [SwaggerSchema(Description = "ID de clasificación")]
        public int IdClasificacion { get; set; }

        [SwaggerSchema(Description = "ID del tipo de costo o gasto")]
        public int IdTipoCostoGasto { get; set; }

        [SwaggerSchema(Description = "ID del sector")]
        public int IdSector { get; set; }

        [SwaggerSchema(Description = "Número de anexo")]
        public string NumeroAnexo { get; set; }

        [SwaggerSchema(Description = "Indica si está posteado")]
        public bool? Posteado { get; set; }

        [SwaggerSchema(Description = "Indica si está anulado")]
        public bool? Anulado { get; set; }

        [SwaggerSchema(Description = "Indica si está eliminado")]
        public bool? Eliminado { get; set; }

        [SwaggerSchema(Description = "Indica si es combustible")]
        public bool? Combustible { get; set; }

        [SwaggerSchema(Description = "Número de serie")]
        public string? NumSerie { get; set; }

        [SwaggerSchema(Description = "IVA retenido")]
        public decimal? IvaRetenido { get; set; }

        [SwaggerSchema(Description = "ID del proveedor")]
        public int IdProveedor { get; set; }

    }

    public class PostearComprasDTO
    {
        [SwaggerSchema(Description = "Id de la compra")]
        public List<int> IdsCompra { get; set; }

    }
}
