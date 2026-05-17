namespace Demo.Domain.Entities;

public class Usuario : BaseEntity
{
    [MaxLength(50)]
    public required string Username { get; set; }

    [MaxLength(100)]
    public required string Email { get; set; }

    [MaxLength(256)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Role identifier embedded in the JWT as the <c>userRoleId</c> claim.
    /// </summary>
    public int RoleId { get; set; } = 1;
}
