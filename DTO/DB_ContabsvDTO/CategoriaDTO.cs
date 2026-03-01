namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    public class CategoriaDTO
    {
        public int IdCategoria { get; set; } 
        public string Nombre { get; set; } 
        public string? Descripcion { get; set; } 
        public bool Estado { get; set; } 
        public DateTime FechaRegistro { get; set; } 
        public int IdCliente { get; set; } 
    }
}
