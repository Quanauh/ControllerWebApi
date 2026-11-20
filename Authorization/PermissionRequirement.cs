using System.Security.Claims;
using HuanBank.Api.DAO;
using Microsoft.AspNetCore.Authorization;

namespace HuanBank.Api.Authorization;

public sealed record PermissionRequirement(string PermissionCode) : IAuthorizationRequirement;

public sealed class PermissionHandler(IAccountDAO accountDao)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (context.User.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(role) &&
            accountDao.HasPermission(role, requirement.PermissionCode))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
