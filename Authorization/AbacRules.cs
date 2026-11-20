namespace HuanBank.Api.Authorization;

public static class AbacActions
{
    public const string AccountAccess = "ACCOUNT_ACCESS";
    public const string Deposit = "DEPOSIT";
    public const string Withdraw = "WITHDRAW";
    public const string Transfer = "TRANSFER";
}

public static class AbacRuleTypes
{
    public const string AccountScope = "ACCOUNT_SCOPE";
    public const string MaxDailyAmount = "MAX_DAILY_AMOUNT";
    public const string MinSingleAmount = "MIN_SINGLE_AMOUNT";
    public const string MaxSingleAmount = "MAX_SINGLE_AMOUNT";
    public const string MaxAfterTime = "MAX_AFTER_TIME";
    public const string MaxDailyCount = "MAX_DAILY_COUNT";
}

public sealed class AbacRule
{
    public string RuleCode { get; set; } = "";
    public string RuleType { get; set; } = "";
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public int? StartMinute { get; set; }
}

public static class AbacEvaluator
{
    public static bool CanApplyAmountRules(
        IReadOnlyCollection<AbacRule> rules,
        decimal amount,
        decimal amountToday)
    {
        if (rules.Count == 0 || amount <= 0)
            return false;

        return rules.All(rule => rule.RuleType switch
        {
            AbacRuleTypes.MaxDailyAmount =>
                rule.NumericValue is >= 0 && amountToday + amount <= rule.NumericValue,
            AbacRuleTypes.MinSingleAmount =>
                rule.NumericValue is >= 0 && amount > rule.NumericValue,
            _ => false
        });
    }

    public static bool CanAccessAccount(
        IReadOnlyCollection<AbacRule> rules,
        string subjectAccount,
        string targetAccount)
    {
        if (rules.Count == 0 || string.IsNullOrWhiteSpace(subjectAccount) ||
            string.IsNullOrWhiteSpace(targetAccount))
            return false;

        return rules.All(rule => rule.RuleType == AbacRuleTypes.AccountScope &&
            (rule.TextValue switch
            {
                "OWN" => string.Equals(subjectAccount, targetAccount, StringComparison.Ordinal),
                "ANY" => true,
                _ => false
            }));
    }

    public static bool CanTransfer(
        IReadOnlyCollection<AbacRule> rules,
        decimal amount,
        decimal transferredToday,
        int transfersToday,
        DateTime businessTime)
    {
        if (rules.Count == 0 || amount <= 0)
            return false;

        var currentMinute = businessTime.Hour * 60 + businessTime.Minute;
        return rules.All(rule => rule.RuleType switch
        {
            AbacRuleTypes.MaxDailyAmount =>
                rule.NumericValue is >= 0 && transferredToday + amount <= rule.NumericValue,
            AbacRuleTypes.MaxSingleAmount =>
                rule.NumericValue is > 0 && amount <= rule.NumericValue,
            AbacRuleTypes.MinSingleAmount =>
                rule.NumericValue is >= 0 && amount > rule.NumericValue,
            AbacRuleTypes.MaxAfterTime =>
                rule.NumericValue is > 0 && rule.StartMinute is >= 0 and <= 1439 &&
                (currentMinute < rule.StartMinute || amount <= rule.NumericValue),
            AbacRuleTypes.MaxDailyCount =>
                rule.NumericValue is > 0 &&
                rule.NumericValue == decimal.Truncate(rule.NumericValue.Value) &&
                transfersToday + 1 <= rule.NumericValue,
            _ => false
        });
    }
}
