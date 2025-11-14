namespace ApiContabsv.DTO.DB_ContabsvDTO
{
    public class MarcaDTO
    {
        public int IdMarca { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public bool? Estado { get; set; }
        public DateTime? Creado { get; set; }
        public int IdCliente { get; set; }
    }
}
