namespace HuanBank.Api.Authorization;

public static class BankRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
}

public static class BankPolicies
{
    public const string AccountAccess = "AccountAccess";
    public const string CustomerOnly = "CustomerOnly";
    public const string AdminOnly = "AdminOnly";
}
