namespace Auth.Domain.Entities;

public sealed class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NombreUsuario { get; set; } = "";
    public string NombreNormalizado { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool Activo { get; set; } = true;
    public Guid RolId { get; set; }
    public Rol Rol { get; set; } = null!;
}
