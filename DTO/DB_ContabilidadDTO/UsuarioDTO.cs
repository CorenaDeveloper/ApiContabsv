namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    public class UsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public bool Estado { get; set; }
        public int IdCliente { get; set; }
        public bool PersonaJuridica { get; set; }
    }
}
