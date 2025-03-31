namespace ApiContabsv.DTO.DB_ContabilidadDTO
{
    public class ResolucionesDTO
    {
        public int Id {  get; set; }
        public int IdTipoDocumento { get; set; }
        public string Documento { get; set; }
        public string NombreCorto { get; set; }
        public string NumeroResolucion { get; set; }
        public string NumeroSerie { get; set; }
        public bool Activo { get; set; }
    } 
    
    public class CreateResolucionesDTO
    {
        public int Id {  get; set; }
        public int IdTipoDocumento { get; set; }
        public string NumeroResolucion { get; set; }
        public string NumeroSerie { get; set; }
        public int IdCliente { get; set; }
    }

    public class UpdateResolucionesDTO
    {
        public int Id { get; set; }
        public int IdTipoDocumento { get; set; }
        public string NumeroResolucion { get; set; }
        public string NumeroSerie { get; set; }
        public bool Activo { get; set; }
    }
}
