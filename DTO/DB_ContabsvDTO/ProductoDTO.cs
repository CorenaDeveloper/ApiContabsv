namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    public class ProductoDTO
    {
        public int IdProducto { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int IdMarca { get; set; }
        public int IdCategoria { get; set; }
        public int IdTipo { get; set; }
        public string? CodigoBarra { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal PrecioVenta { get; set; }
        public bool Estado { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
        public int IdCliente { get; set; }
        public int Stock { get; set; } = 0;
        public int StockMinimo { get; set; } = 0;
        public int StockMaximo { get; set; } = 0;
        public decimal? Peso { get; set; }
        public decimal? Volumen { get; set; }
        public string UnidadMedida { get; set; } = "";
        public string? Imagen { get; set; } = "";
        public string Sku { get; set; } = "";
        public string nombreMarca { get; set; } = "";
        public string nombreCategoria { get; set; } = "";
        public string nombreTipo { get; set; } = "";
        public int TipoItemId { get; set; } = 0;
        public int FactorCaja { get; set; } = 1;
        public int CodigoUnidadMh { get; set; } = 59;

    }

}
