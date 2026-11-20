using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using HuanBank.Api.DAO.Models;
using HuanBank.Api.Exceptions;
using HuanBank.Api.Authorization;
namespace HuanBank.Api.DAO;
using HuanBank.Api.Util;
class AccountDAO : IAccountDAO
{
    private readonly Database _database;

    public AccountDAO(Database database) => _database = database;

    // Kiem tra ton tai
    public bool Exists(string AccountNumber)
    {
        using OracleConnection conn = _database.GetConnection();
        int dem = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM ACCOUNTS WHERE ACCOUNT_NUMBER=:AccountNumber", new { AccountNumber });
        return dem > 0;
    }

    public LoginAccountRecord? GetLoginAccount(string accountNumber)
    {
        using OracleConnection conn = _database.GetConnection();
        return conn.QuerySingleOrDefault<LoginAccountRecord>(
            "SELECT PASSWORD, ROLE, STATUS FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :accountNumber",
            new { accountNumber });
    }

    public RoleRecord? GetActiveRole(string accountNumber)
    {
        using OracleConnection conn = _database.GetConnection();
        return conn.QuerySingleOrDefault<RoleRecord>(
            @"SELECT r.ROLE_NAME AS RoleName, r.DESCRIPTION AS Description
              FROM ACCOUNTS a
              JOIN ROLES r ON r.ROLE_NAME = a.ROLE
              WHERE a.ACCOUNT_NUMBER = :accountNumber AND a.STATUS = 'ACTIVE'",
            new { accountNumber });
    }

    public RoleRecord? GetRole(string roleName)
    {
        using OracleConnection conn = _database.GetConnection();
        return conn.QuerySingleOrDefault<RoleRecord>(
            @"SELECT ROLE_NAME AS RoleName, DESCRIPTION AS Description
              FROM ROLES WHERE ROLE_NAME = :roleName", new { roleName });
    }

    public bool HasPermission(string role, string permissionCode)
    {
        using OracleConnection conn = _database.GetConnection();
        return conn.ExecuteScalar<int>(
            @"SELECT COUNT(*) FROM ROLE_PERMISSIONS
              WHERE ROLE_NAME = :role AND PERMISSION_CODE = :permissionCode",
            new { role, permissionCode }) > 0;
    }

    public IReadOnlyList<AbacRule> GetAbacRules(string role, string actionCode)
    {
        using OracleConnection conn = _database.GetConnection();
        return LoadAbacRules(conn, null, role, actionCode);
    }

    private static IReadOnlyList<AbacRule> LoadAbacRules(
        OracleConnection conn, OracleTransaction? transaction, string role, string actionCode) =>
        conn.Query<AbacRule>(
            @"SELECT RULE_CODE AS RuleCode, RULE_TYPE AS RuleType,
                     NUMERIC_VALUE AS NumericValue, TEXT_VALUE AS TextValue,
                     START_MINUTE AS StartMinute
              FROM ABAC_RULES
              WHERE ROLE_NAME = :role AND ACTION_CODE = :actionCode AND IS_ACTIVE = 'Y'
              ORDER BY RULE_CODE",
            new { role, actionCode }, transaction).AsList();

    private sealed class AccountBalanceState
    {
        public string Role { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Balance { get; set; }
    }

    private sealed class TransferDailyUsage
    {
        public decimal TotalAmount { get; set; }
        public int TransferCount { get; set; }
    }

    private static DateTime GetBusinessTime(OracleConnection conn, OracleTransaction transaction) =>
        conn.ExecuteScalar<DateTime>(
            "SELECT CAST(SYSTIMESTAMP AT TIME ZONE '+07:00' AS DATE) FROM DUAL",
            transaction: transaction);

    private static decimal GetDailyAmount(
        OracleConnection conn, OracleTransaction transaction, string accountNumber,
        string transactionType, DateTime businessTime)
    {
        var dayStart = businessTime.Date;
        return conn.ExecuteScalar<decimal>(
            @"SELECT NVL(SUM(AMOUNT), 0)
              FROM TRANSACTIONS
              WHERE ACCOUNT_NUMBER = :accountNumber
                AND TRANSACTION_TYPE = :transactionType
                AND TRANSACTION_DATE >= :dayStart
                AND TRANSACTION_DATE < :nextDay",
            new { accountNumber, transactionType, dayStart, nextDay = dayStart.AddDays(1) },
            transaction);
    }

    private int GetNextId(OracleConnection conn, OracleTransaction tran, string tenSequence)
    {
        return conn.ExecuteScalar<int>($"SELECT {tenSequence}.NEXTVAL FROM dual", transaction: tran);
    }

    // Tao ti khoan
    public void CreateAccount(CreateAccountData account)
    {
        using OracleConnection conn = _database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            int customerId = GetNextId(conn, trann, "seq_customers");

            conn.Execute(
                @"INSERT INTO CUSTOMERS (CUSTOMER_ID, FULL_NAME, PHONE, EMAIL, DATE_OF_BIRTH, ADDRESS)
                  VALUES (:customerId, :name, :phone, :email, TO_DATE(:birth,'dd/MM/YYYY'), :address)",
                new
                {
                    customerId,
                    name = account.Name,
                    phone = account.Phone,
                    email = account.Email,
                    birth = account.DateOfBirth,
                    address = account.Address
                },
                trann);

            conn.Execute(
                "INSERT INTO ACCOUNTS (ACCOUNT_NUMBER, CUSTOMER_ID, PASSWORD, BALANCE, STATUS, ROLE) VALUES (:AccountNumber, :customerId, :Password, 0, 'ACTIVE', 'Customer')",
                new { account.AccountNumber, customerId, Password = account.Password }, trann);

            trann.Commit();
        }
        catch (OracleException ex)
        {
            trann.Rollback();

            if (ex.Message.Contains("UQ_KHACHHANG_SDT", StringComparison.OrdinalIgnoreCase))
            {
                throw new PhoneAlreadyExistsException(ex);
            }

            if (ex.Message.Contains("UQ_KHACHHANG_EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                throw new EmailAlreadyExistsException(ex);
            }

            if (ex.Message.Contains("PK_TAI_KHOAN", StringComparison.OrdinalIgnoreCase))
            {
                throw new AccountAlreadyExistsException(ex);
            }

            throw;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    // Cong/tru so du, tra ve ca so du truoc va sau (dung RETURNING cho so du sau)
    private (decimal before, decimal after) UpdateBalance(OracleConnection conn, OracleTransaction tran, string AccountNumber, decimal delta)
    {
        decimal before = conn.ExecuteScalar<decimal>(
            "SELECT BALANCE FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber FOR UPDATE", new { AccountNumber }, tran);

        var p = new DynamicParameters();
        p.Add(":delta", delta);
        p.Add(":AccountNumber", AccountNumber);
        p.Add(":soDuMoi", dbType: DbType.Decimal, direction: ParameterDirection.Output);

        conn.Execute(
            "UPDATE ACCOUNTS SET BALANCE = BALANCE + :delta WHERE ACCOUNT_NUMBER = :AccountNumber RETURNING BALANCE INTO :soDuMoi",
            p, tran);

        decimal after = p.Get<decimal>(":soDuMoi");
        return (before, after);
    }

    // Chuyen tien - chi 1 dong TRANSACTIONS, chi track so du ben khoi tao (stk1)
    public TransferExecutionResult TryTransferMoney(string AccountNumber1, string AccountNumber2, decimal a)
    {
        using OracleConnection conn = _database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            conn.Query<string>(
                @"SELECT ACCOUNT_NUMBER
                  FROM ACCOUNTS
                  WHERE ACCOUNT_NUMBER IN (:AccountNumber1, :AccountNumber2)
                  ORDER BY ACCOUNT_NUMBER
                  FOR UPDATE",
                new { AccountNumber1, AccountNumber2 }, trann).AsList();

            var sender = conn.QuerySingle<AccountBalanceState>(
                "SELECT ROLE AS Role, STATUS AS Status, BALANCE AS Balance FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber",
                new { AccountNumber = AccountNumber1 }, trann);
            if (sender.Balance < a)
            {
                trann.Rollback();
                return TransferExecutionResult.InsufficientBalance;
            }

            // The source row is locked above: two transfers from one account cannot
            // both pass a daily limit using the same prior total.
            var canTransfer = sender.Status == "ACTIVE" &&
                conn.ExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM ROLE_PERMISSIONS
                      WHERE ROLE_NAME = :role AND PERMISSION_CODE = :permissionCode",
                    new { role = sender.Role, permissionCode = BankPermissions.Transfer }, trann) > 0;
            var businessTime = GetBusinessTime(conn, trann);
            var dayStart = businessTime.Date;
            var usage = conn.QuerySingle<TransferDailyUsage>(
                @"SELECT NVL(SUM(AMOUNT), 0) AS TotalAmount,
                         COUNT(*) AS TransferCount
                  FROM TRANSACTIONS
                  WHERE ACCOUNT_NUMBER = :accountNumber
                    AND TRANSACTION_TYPE = 'TRANSFER_OUT'
                    AND TRANSACTION_DATE >= :dayStart
                    AND TRANSACTION_DATE < :nextDay",
                new { accountNumber = AccountNumber1, dayStart, nextDay = dayStart.AddDays(1) }, trann);
            if (!canTransfer || !AbacEvaluator.CanTransfer(
                    LoadAbacRules(conn, trann, sender.Role, AbacActions.Transfer),
                    a, usage.TotalAmount, usage.TransferCount, businessTime))
            {
                trann.Rollback();
                return TransferExecutionResult.PolicyDenied;
            }

            var (before1, after1) = UpdateBalance(conn, trann, AccountNumber1, -a);
            var (before2, after2) = UpdateBalance(conn, trann, AccountNumber2, a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");

            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER, TRANSACTION_DATE)
              VALUES (:transactionId, :AccountNumber1, :AccountNumber2, 'TRANSFER_OUT', :a, :before1, :after1, :businessTime)",
                new { transactionId, AccountNumber1, AccountNumber2, a, before1, after1, businessTime }, trann);

            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER, TRANSACTION_DATE)
              VALUES (:transactionId, :AccountNumber2, :AccountNumber1, 'TRANSFER_IN', :a, :before2, :after2, :businessTime)",
                new { transactionId, AccountNumber2, AccountNumber1, a, before2, after2, businessTime }, trann);

            trann.Commit();
            return TransferExecutionResult.Success;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    // Nap tien
    public BalanceOperationResult Deposit(string AccountNumber, decimal a)
    {
        using OracleConnection conn = _database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            var account = conn.QuerySingle<AccountBalanceState>(
                @"SELECT ROLE AS Role, STATUS AS Status, BALANCE AS Balance
                  FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber FOR UPDATE",
                new { AccountNumber }, trann);
            var businessTime = GetBusinessTime(conn, trann);
            var amountToday = GetDailyAmount(conn, trann, AccountNumber, "DEPOSIT", businessTime);
            if (account.Status != "ACTIVE" || !AbacEvaluator.CanApplyAmountRules(
                    LoadAbacRules(conn, trann, account.Role, AbacActions.Deposit), a, amountToday))
            {
                trann.Rollback();
                return BalanceOperationResult.PolicyDenied;
            }

            var (before, after) = UpdateBalance(conn, trann, AccountNumber, a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");
            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER, TRANSACTION_DATE)
                  VALUES (:transactionId, :AccountNumber, NULL, 'DEPOSIT', :a, :before, :after, :businessTime)",
                new { transactionId, AccountNumber, a, before, after, businessTime }, trann);

            trann.Commit();
            return BalanceOperationResult.Success;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    // Rut tien
    public BalanceOperationResult TryWithdraw(string AccountNumber, decimal a)
    {
        using OracleConnection conn = _database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            var account = conn.QuerySingle<AccountBalanceState>(
                @"SELECT ROLE AS Role, STATUS AS Status, BALANCE AS Balance
                  FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber FOR UPDATE",
                new { AccountNumber }, trann);
            if (account.Balance < a)
            {
                trann.Rollback();
                return BalanceOperationResult.InsufficientBalance;
            }

            var businessTime = GetBusinessTime(conn, trann);
            var amountToday = GetDailyAmount(conn, trann, AccountNumber, "WITHDRAW", businessTime);
            if (account.Status != "ACTIVE" || !AbacEvaluator.CanApplyAmountRules(
                    LoadAbacRules(conn, trann, account.Role, AbacActions.Withdraw), a, amountToday))
            {
                trann.Rollback();
                return BalanceOperationResult.PolicyDenied;
            }

            var (before, after) = UpdateBalance(conn, trann, AccountNumber, -a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");
            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER, TRANSACTION_DATE)
                  VALUES (:transactionId, :AccountNumber, NULL, 'WITHDRAW', :a, :before, :after, :businessTime)",
                new { transactionId, AccountNumber, a, before, after, businessTime }, trann);

            trann.Commit();
            return BalanceOperationResult.Success;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    public decimal GetBalance(string AccountNumber)
    {
        using OracleConnection conn = _database.GetConnection();
        return conn.ExecuteScalar<decimal>(
            "SELECT BALANCE FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber", new { AccountNumber });
    }

    public AccountRecord? GetAccountInfor(string AccountNumber)
    {
        using OracleConnection conn = _database.GetConnection();

        string sql = @"SELECT a.ACCOUNT_NUMBER as AccountNumber, c.FULL_NAME as FullName, c.PHONE, c.EMAIL, c.DATE_OF_BIRTH as DateOfBirth, c.ADDRESS, a.BALANCE
                       FROM ACCOUNTS a
                       JOIN CUSTOMERS c ON a.CUSTOMER_ID = c.CUSTOMER_ID
                       WHERE a.ACCOUNT_NUMBER = :AccountNumber";

        return conn.QueryFirstOrDefault<AccountRecord>(sql, new { AccountNumber });
    }

    public List<TransactionHistoryRecord> GetTransactionHistory(string AccountNumber)
    {
        using OracleConnection conn = _database.GetConnection();
        string sql = @"SELECT ACCOUNT_NUMBER as AccountNumber , COUNTERPART_ACCOUNT_NUMBER as CounterAccount, TRANSACTION_TYPE as TransactionType, AMOUNT,BALANCE_BEFORE as BalanceBefore, BALANCE_AFTER as BalanceAfter, TRANSACTION_DATE as TransactionDate
                        FROM TRANSACTIONS
                        WHERE ACCOUNT_NUMBER = :AccountNumber
                        ORDER BY TRANSACTION_DATE ASC";

        return conn.Query<TransactionHistoryRecord>(sql, new { AccountNumber }).ToList();
    }
}
