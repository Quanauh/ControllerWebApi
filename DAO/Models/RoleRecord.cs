namespace HuanBank.Api.DAO.Models;

/// <summary>One row from the application ROLES table.</summary>
public sealed class RoleRecord
{
    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
}
