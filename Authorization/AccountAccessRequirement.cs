using System.Security.Claims;
using HuanBank.Api.DAO;
using Microsoft.AspNetCore.Authorization;

namespace HuanBank.Api.Authorization;

/// <summary>
/// Applies the account scope rules configured for the current role in Oracle.
/// </summary>
public sealed class AccountAccessRequirement : IAuthorizationRequirement { }

public sealed class AccountAccessHandler(IHttpContextAccessor httpContextAccessor, IAccountDAO accountDao)
    : AuthorizationHandler<AccountAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AccountAccessRequirement requirement)
    {
        var routeAccount = httpContextAccessor.HttpContext?.Request.RouteValues["accountNumber"]?.ToString();
        if (string.IsNullOrWhiteSpace(routeAccount))
            return Task.CompletedTask;

        var role = context.User.FindFirstValue(ClaimTypes.Role);
        var subjectAccount = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(role) &&
            !string.IsNullOrWhiteSpace(subjectAccount) &&
            AbacEvaluator.CanAccessAccount(
                accountDao.GetAbacRules(role, AbacActions.AccountAccess),
                subjectAccount, routeAccount))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
