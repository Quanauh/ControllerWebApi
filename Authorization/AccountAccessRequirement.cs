using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HuanBank.Api.Authorization;

/// <summary>
/// ABAC rule: a customer may access only their own account; an admin may access any account.
/// The account resource must be supplied by the route.
/// </summary>
public sealed class AccountAccessRequirement : IAuthorizationRequirement { }

public sealed class AccountAccessHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<AccountAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountAccessRequirement requirement)
    {
        var routeAccount = httpContextAccessor.HttpContext?.Request.RouteValues["accountNumber"]?.ToString();
        if (string.IsNullOrWhiteSpace(routeAccount))
            return Task.CompletedTask;

        if (context.User.IsInRole(BankRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var subjectAccount = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(subjectAccount) &&
            !string.IsNullOrWhiteSpace(routeAccount) &&
            string.Equals(subjectAccount, routeAccount, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
