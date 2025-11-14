namespace ApiContabsv.DTO.DB_SeguridadDTO
{
    public class UsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public string? Usuario { get; set; }
        public bool Estado { get; set; }
        public int idCliente { get; set; }
        public int idRol { get; set; }
    }

    public class CreateUsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public string? Usuario { get; set; }
        public string Pass { get; set; }
        public int idCliente { get; set; }
        public int idRol { get; set; }
    }
    public class UpdateUsuarioDTO
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public string? Usuario { get; set; }
        public int IdRol { get; set; }
        public bool Estado { get; set; }
        public int IdCliente { get; set; }
    }

    public class CambiarPasswordDTO
    {
        public int IdUsuario { get; set; }
        public string PasswordActual { get; set; } = "";
        public string PasswordNuevo { get; set; } = "";
    }
}
