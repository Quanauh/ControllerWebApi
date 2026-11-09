using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HuanBank.Api.Authorization;

/// <summary>
/// ABAC rule: a customer may access only their own account; an admin may access any account.
/// The account resource is taken from the route when present, otherwise from the authenticated subject.
/// </summary>
public sealed class AccountAccessRequirement : IAuthorizationRequirement { }

public sealed class AccountAccessHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<AccountAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountAccessRequirement requirement)
    {
        if (context.User.IsInRole(BankRoles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var subjectAccount = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var routeAccount = httpContextAccessor.HttpContext?.Request.RouteValues["accountNumber"]?.ToString();
        var resourceAccount = routeAccount ?? subjectAccount;

        if (!string.IsNullOrWhiteSpace(subjectAccount) &&
            string.Equals(subjectAccount, resourceAccount, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
