using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using HuanBank.Api.DAO.Models;
using HuanBank.Api.Exceptions;
namespace HuanBank.Api.DAO;
using HuanBank.Api.Util;
class AccountDAO : IAccountDAO
{
    // Kiem tra ton tai
    public bool Exists(string AccountNumber)
    {
        using OracleConnection conn = Database.GetConnection();
        int dem = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM ACCOUNTS WHERE ACCOUNT_NUMBER=:AccountNumber", new { AccountNumber });
        return dem > 0;
    }

    public bool checkPassWord(string AccountNumber, string Password)
    {
        using OracleConnection conn = Database.GetConnection();
        int dem = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM ACCOUNTS WHERE ACCOUNT_NUMBER=:AccountNumber AND PASSWORD=:Password", new { AccountNumber, Password });
        return dem > 0;
    }

    private int GetNextId(OracleConnection conn, OracleTransaction tran, string tenSequence)
    {
        return conn.ExecuteScalar<int>($"SELECT {tenSequence}.NEXTVAL FROM dual", transaction: tran);
    }

    // Tao ti khoan
    public void CreateAccount(CreateAccountData account)
    {
        using OracleConnection conn = Database.GetConnection();
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
                "INSERT INTO ACCOUNTS (ACCOUNT_NUMBER, CUSTOMER_ID, PASSWORD, BALANCE) VALUES (:AccountNumber, :customerId, :Password, 0)",
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
    public bool TryTransferMoney(string AccountNumber1, string AccountNumber2, decimal a)
    {
        using OracleConnection conn = Database.GetConnection();
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

            decimal senderBalance = conn.ExecuteScalar<decimal>(
                "SELECT BALANCE FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber",
                new { AccountNumber = AccountNumber1 }, trann);
            if (senderBalance < a)
            {
                trann.Rollback();
                return false;
            }

            var (before1, after1) = UpdateBalance(conn, trann, AccountNumber1, -a);
            var (before2, after2) = UpdateBalance(conn, trann, AccountNumber2, a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");

            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER)
              VALUES (:transactionId, :AccountNumber1, :AccountNumber2, 'TRANSFER_OUT', :a, :before1, :after1)",
                new { transactionId, AccountNumber1, AccountNumber2, a, before1, after1 }, trann);

            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER)
              VALUES (:transactionId, :AccountNumber2, :AccountNumber1, 'TRANSFER_IN', :a, :before2, :after2)",
                new { transactionId, AccountNumber2, AccountNumber1, a, before2, after2 }, trann);

            trann.Commit();
            return true;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    // Nap tien
    public void Deposit(string AccountNumber, decimal a)
    {
        using OracleConnection conn = Database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            var (before, after) = UpdateBalance(conn, trann, AccountNumber, a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");
            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER)
                  VALUES (:transactionId, :AccountNumber, NULL, 'DEPOSIT', :a, :before, :after)",
                new { transactionId, AccountNumber, a, before, after }, trann);

            trann.Commit();
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    // Rut tien
    public bool TryWithdraw(string AccountNumber, decimal a)
    {
        using OracleConnection conn = Database.GetConnection();
        using OracleTransaction trann = conn.BeginTransaction();
        try
        {
            decimal balance = conn.ExecuteScalar<decimal>(
                "SELECT BALANCE FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber FOR UPDATE",
                new { AccountNumber }, trann);
            if (balance < a)
            {
                trann.Rollback();
                return false;
            }

            var (before, after) = UpdateBalance(conn, trann, AccountNumber, -a);

            int transactionId = GetNextId(conn, trann, "seq_transactions");
            conn.Execute(
                @"INSERT INTO TRANSACTIONS (TRANSACTION_ID, ACCOUNT_NUMBER, COUNTERPART_ACCOUNT_NUMBER, TRANSACTION_TYPE, AMOUNT, BALANCE_BEFORE, BALANCE_AFTER)
                  VALUES (:transactionId, :AccountNumber, NULL, 'WITHDRAW', :a, :before, :after)",
                new { transactionId, AccountNumber, a, before, after }, trann);

            trann.Commit();
            return true;
        }
        catch
        {
            trann.Rollback();
            throw;
        }
    }

    public decimal GetBalance(string AccountNumber)
    {
        using OracleConnection conn = Database.GetConnection();
        return conn.ExecuteScalar<decimal>(
            "SELECT BALANCE FROM ACCOUNTS WHERE ACCOUNT_NUMBER = :AccountNumber", new { AccountNumber });
    }

    public AccountRecord? GetAccountInfor(string AccountNumber)
    {
        using OracleConnection conn = Database.GetConnection();

        string sql = @"SELECT a.ACCOUNT_NUMBER as AccountNumber, c.FULL_NAME as FullName, c.PHONE, c.EMAIL, c.DATE_OF_BIRTH as DateOfBirth, c.ADDRESS, a.BALANCE
                       FROM ACCOUNTS a
                       JOIN CUSTOMERS c ON a.CUSTOMER_ID = c.CUSTOMER_ID
                       WHERE a.ACCOUNT_NUMBER = :AccountNumber";

        return conn.QueryFirstOrDefault<AccountRecord>(sql, new { AccountNumber });
    }

    public List<TransactionHistoryRecord> GetTransactionHistory(string AccountNumber)
    {
        using OracleConnection conn = Database.GetConnection();
        string sql = @"SELECT ACCOUNT_NUMBER as AccountNumber , COUNTERPART_ACCOUNT_NUMBER as CounterAccount, TRANSACTION_TYPE as TransactionType, AMOUNT,BALANCE_BEFORE as BalanceBefore, BALANCE_AFTER as BalanceAfter, TRANSACTION_DATE as TransactionDate
                        FROM TRANSACTIONS
                        WHERE ACCOUNT_NUMBER = :AccountNumber
                        ORDER BY TRANSACTION_DATE ASC";

        return conn.Query<TransactionHistoryRecord>(sql, new { AccountNumber }).ToList();
    }
}
