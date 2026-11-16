namespace HuanBank.Api.Services.Models;

public sealed record BusinessResponse(long Code, string Message);

public static class TransactionCodes
{
    public const long Success = 1;
    public const long AccountNotFound = -1;
    public const long InvalidAmount = -2;
    public const long InsufficientBalance = -3;
    public const long CannotTransferToSelf = -4;
}
