using Swashbuckle.AspNetCore.Annotations;

namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    public class ProveedoresDTO
    {
        [SwaggerSchema(Description = "ID del proveedor")]
        public int IdProveedor { get; set; }

        [SwaggerSchema(Description = "Nombres del cliente o proveedor")]
        public string Nombres { get; set; }

        [SwaggerSchema(Description = "Apellidos del cliente o proveedor")]
        public string Apellidos { get; set; }

        [SwaggerSchema(Description = "Indica si es persona jurídica (true) o natural (false)")]
        public bool PersonaJuridica { get; set; }

        [SwaggerSchema(Description = "Nombre de la razón social en caso de persona jurídica")]
        public string NombreRazonSocial { get; set; }

        [SwaggerSchema(Description = "Nombre comercial de la empresa o negocio")]
        public string NombreComercial { get; set; }

        [SwaggerSchema(Description = "DUI del cliente")]
        public string DuiCliente { get; set; }

        [SwaggerSchema(Description = "Nombre del representante legal en caso de persona jurídica")]
        public string RepresentanteLegal { get; set; }

        [SwaggerSchema(Description = "DUI del representante legal")]
        public string DuiRepresentanteLegal { get; set; }

        [SwaggerSchema(Description = "Teléfono fijo del cliente o proveedor")]
        public string TelefonoCliente { get; set; }

        [SwaggerSchema(Description = "Número de celular del cliente o proveedor")]
        public string Celular { get; set; }

        [SwaggerSchema(Description = "Número de Registro de Contribuyente (NRC)")]
        public string Nrc { get; set; }

        [SwaggerSchema(Description = "Número de Identificación Tributaria (NIT) del proveedor")]
        public string NitProveedor { get; set; }

        [SwaggerSchema(Description = "ID del cliente")]
        public int IdCliente { get; set; }

        [SwaggerSchema(Description = "Correo electrónico del cliente o proveedor")]
        public string Email { get; set; }

        [SwaggerSchema(Description = "Dirección del cliente o proveedor")]
        public string Direccion { get; set; }

        [SwaggerSchema(Description = "ID del sector al que pertenece el cliente o proveedor")]
        public int IdSector { get; set; }

        [SwaggerSchema(Description = "Fecha de creación del registro")]
        public DateTime Creado { get; set; }

        [SwaggerSchema(Description = "Tipo de contribuyente del cliente o proveedor")]
        public string TipoContribuyente { get; set; }
    }
}
