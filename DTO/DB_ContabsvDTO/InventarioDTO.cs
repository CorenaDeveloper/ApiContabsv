namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    public class InventarioDTO
    {
        public int idMovimiento { get; set; } = 0;
        public int idProducto { get; set; } = 0;
        public int idCliente { get; set; } = 0;

        // Tipo de movimiento
        public string tipoMovimiento { get; set; } = "";
        // Valores: Entrada, Salida, Ajuste, Merma, Devolucion

        // Cantidad (positivo = entrada, negativo = salida)
        public int cantidad { get; set; } = 0;

        // Precios en ese momento
        public decimal? costoUnitario { get; set; }
        public decimal? precioVentaUnitario { get; set; }

        // Información adicional
        public string? lote { get; set; } = "";
        public string? numeroSerie { get; set; } = "";
        public string? ubicacion { get; set; } = "";

        // Referencias
        public int? idCompra { get; set; } = 0;
        public int? idVenta { get; set; } = 0;
        public string? numeroDocumento { get; set; } = "";

        // Seguimiento
        public string? motivoMovimiento { get; set; } = "";
        public string? responsable { get; set; } = "";
        public DateTime fechaMovimiento { get; set; } = DateTime.Now;

        // Notas
        public string? observaciones { get; set; } = "";

        // Propiedades adicionales para joins (cuando listes movimientos con datos relacionados)
        public string? nombreProducto { get; set; } = "";
        public string? skuProducto { get; set; } = "";
        public string? imagenProducto { get; set; } = "";
        public int? stockAnterior { get; set; } = 0;
        public int? stockNuevo { get; set; } = 0;
    }
}
