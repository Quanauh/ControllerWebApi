using System.Security.Claims;
using HuanBank.Api.Authorization;
using HuanBank.Api.DAO;
using HuanBank.Api.DAO.Models;
using HuanBank.Api.Result;
using HuanBank.Api.Services;
using HuanBank.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

AssertAllowed(BankRoles.Customer, "C100", "C100", true);
AssertAllowed(BankRoles.Customer, "C100", "C200", false);
AssertAllowed(BankRoles.Customer, "C100", null, false);
AssertAllowed(BankRoles.Admin, "A100", "C200", true);
AssertAllowed(BankRoles.Admin, "A100", null, false);
AssertLogin(new() { Password = "pw", Role = BankRoles.Customer, Status = "ACTIVE" }, "pw", LoginResult.Success, BankRoles.Customer);
AssertLogin(new() { Password = "pw", Role = BankRoles.Admin, Status = "ACTIVE" }, "pw", LoginResult.Success, BankRoles.Admin);
AssertLogin(new() { Password = "pw", Role = BankRoles.Admin, Status = "LOCKED" }, "pw", LoginResult.AccountInactive, null);
AssertLogin(new() { Password = "pw", Role = "Unknown", Status = "ACTIVE" }, "pw", LoginResult.InvalidRole, null);
AssertLogin(new() { Password = "pw", Role = BankRoles.Customer, Status = "ACTIVE" }, "wrong", LoginResult.IncorrectPassword, null);

Console.WriteLine("Access control smoke checks passed.");

static void AssertAllowed(string role, string subject, string? target, bool expected)
{
    var httpContext = new DefaultHttpContext();
    if (target != null)
        httpContext.Request.RouteValues["accountNumber"] = target;

    var accessor = new HttpContextAccessor { HttpContext = httpContext };
    var handler = new AccountAccessHandler(accessor);
    var user = new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, subject), new Claim(ClaimTypes.Role, role)],
        "test"));
    var requirement = new AccountAccessRequirement();
    var context = new AuthorizationHandlerContext([requirement], user, null);

    handler.HandleAsync(context).GetAwaiter().GetResult();
    if (context.HasSucceeded != expected)
        throw new Exception($"Unexpected access result for {role}, target={target ?? "<none>"}.");
}

static void AssertLogin(LoginAccountRecord account, string password, LoginResult expectedResult, string? expectedRole)
{
    var service = new BankService(new StubDao(account), NullLogger<BankService>.Instance,
        new RegisterAccountValidator(), new MoneyTransactionValidator());
    var actual = service.Login("C100", password);
    if (actual.Result != expectedResult || actual.Role != expectedRole)
        throw new Exception($"Unexpected login result: {actual.Result}, role={actual.Role}.");
}

sealed class StubDao(LoginAccountRecord account) : IAccountDAO
{
    public bool Exists(string accountNumber) => true;
    public LoginAccountRecord? GetLoginAccount(string accountNumber) => account;
    public string? GetActiveRole(string accountNumber) => account.Status == "ACTIVE" ? account.Role : null;
    public void CreateAccount(CreateAccountData data) => throw new NotImplementedException();
    public void Deposit(string accountNumber, decimal amount) => throw new NotImplementedException();
    public bool TryWithdraw(string accountNumber, decimal amount) => throw new NotImplementedException();
    public bool TryTransferMoney(string source, string target, decimal amount) => throw new NotImplementedException();
    public decimal GetBalance(string accountNumber) => throw new NotImplementedException();
    public AccountRecord? GetAccountInfor(string accountNumber) => throw new NotImplementedException();
    public List<TransactionHistoryRecord> GetTransactionHistory(string accountNumber) => throw new NotImplementedException();
}
