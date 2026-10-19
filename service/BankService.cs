using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using HuanBank.Api.DAO;
using HuanBank.Api.DAO.Models;
using HuanBank.Api.Validators;
using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;

namespace HuanBank.Api.Services;

public class BankService : IBankService
{
    private readonly IAccountDAO _acd;
    private readonly ILogger<BankService> _logger;
    private readonly IAccountValidator _validator;

    public BankService(IAccountDAO acd, ILogger<BankService> logger, IAccountValidator validator)
    {
        _acd = acd;
        _logger = logger;
        _validator = validator;
    }

    public bool AccountExists(string AccountNumber)
    {
        return _acd.Exists(AccountNumber);
    }

    // Dang ky
    public TransactionResult CreateAccount(RegisterAccountCommand command)
    {
        var validation = _validator.ValidateRegistration(command);
        if (validation != TransactionResult.Success)
        {
            _logger.LogWarning("Dang ky - Validate that bai: {Result}", validation);
            return validation;
        }

        if (_acd.Exists(command.AccountNumber))
        {
            _logger.LogWarning("Dang Ki - Sotk {AccountNumber} da ton tai", command.AccountNumber);
            return TransactionResult.AccountAlreadyExists;
        }

        _acd.CreateAccount(new CreateAccountData
        {
            Name = command.Name,
            Phone = command.Phone,
            Email = command.Email,
            DateOfBirth = command.DateOfBirth,
            Address = command.Address,
            AccountNumber = command.AccountNumber,
            Password = command.Password
        });
        _logger.LogInformation("Tao Tai Khoan - Tao tai khoan {AccountNumber} thanh cong", command.AccountNumber);
        return TransactionResult.Success;
    }

    // Dang nhap
    public TransactionResult Login(string AccountNumber, string Password)
    {
        if (!AccountExists(AccountNumber))
        {
            _logger.LogWarning("So tai khoan {AccountNumber} khong ton tai", AccountNumber);
            return TransactionResult.AccountNotFound;
        }

        if (!_acd.checkPassWord(AccountNumber, Password))
        {
            _logger.LogWarning("Dang Nhap - So tai khoan {AccountNumber} nhap khong dung mat khau", AccountNumber);
            return TransactionResult.IncorrectPassword;
        }

        _logger.LogInformation("Dang Nhap - Stk {AccountNumber} dang nhap thanh cong", AccountNumber);
        return TransactionResult.Success;
    }

    // Nap tien
    public TransactionResult Deposit(string AccountNumber, decimal a)
    {
        if (!AccountExists(AccountNumber))
        {
            _logger.LogWarning("Nap tien - So tai khoan {AccountNumber} khong ton tai, nap tien that bai", AccountNumber);
            return TransactionResult.AccountNotFound;
        }

        var validation = _validator.ValidateAmount(a);
        if (validation != TransactionResult.Success)
        {
            _logger.LogWarning("Nap tien - So tai khoan {AccountNumber} nap tien that bai, so tien {Amount} khong hop le", AccountNumber, a);
            return validation;
        }

        _acd.Deposit(AccountNumber, a);
        _logger.LogInformation("Nap Tien - So tai khoan {AccountNumber} nap tien thanh cong, so tien {Amount}", AccountNumber, a);
        return TransactionResult.Success;
    }

    // Rut tien
    public TransactionResult Withdraw(string AccountNumber, decimal a)
    {
        if (!AccountExists(AccountNumber))
        {
            _logger.LogWarning("Rut tien - So tai khoan {AccountNumber} khong ton tai, rut tien that bai", AccountNumber);
            return TransactionResult.AccountNotFound;
        }

        var validation = _validator.ValidateWithdraw(a, _acd.GetBalance(AccountNumber));
        if (validation != TransactionResult.Success)
        {
            _logger.LogWarning("Rut tien - So tai khoan {AccountNumber} rut tien that bai, ket qua validate: {Result}", AccountNumber, validation);
            return validation;
        }

        _acd.Withdraw(AccountNumber, a);
        _logger.LogInformation("Rut tien - So tai khoan {AccountNumber} rut tien thanh cong", AccountNumber);
        return TransactionResult.Success;
    }

    // Chuyen tien
    public TransactionResult TransferMoney(string AccountNumber1, string AccountNumber2, decimal a)
    {
        if (!AccountExists(AccountNumber2))
        {
            _logger.LogWarning("Chuyen tien - So tai khoan {AccountNumber2} khong ton tai, chuyen tien that bai", AccountNumber2);
            return TransactionResult.AccountNotFound;
        }

        var validation = _validator.ValidateTransfer(AccountNumber1, AccountNumber2, a, _acd.GetBalance(AccountNumber1));
        if (validation != TransactionResult.Success)
        {
            _logger.LogWarning("Chuyen tien - So tai khoan {AccountNumber1} chuyen tien that bai, ket qua validate: {Result}", AccountNumber1, validation);
            return validation;
        }

        _acd.TransferMoney(AccountNumber1, AccountNumber2, a);
        _logger.LogInformation("Chuyen tien - So tai khoan {AccountNumber1} chuyen tien thanh cong cho so tai khoan {AccountNumber2}, So tien {Amount}", AccountNumber1, AccountNumber2, a);
        return TransactionResult.Success;
    }

    public decimal GetBalance(string AccountNumber)
    {
        decimal balance = _acd.GetBalance(AccountNumber);
        _logger.LogInformation("Xem so du - Xem so du {AccountNumber} thanh cong", AccountNumber);
        return balance;
    }

    public AccountDetails? GetAccountInfor(string AccountNumber)
    {
        AccountRecord? accountInfo = _acd.GetAccountInfor(AccountNumber);
        _logger.LogInformation("Xem thong tin - Xem thong tin {AccountNumber} thanh cong", AccountNumber);
        if (accountInfo == null)
        {
            return null;
        }

        return new AccountDetails
        {
            AccountNumber = accountInfo.AccountNumber,
            FullName = accountInfo.FullName,
            Phone = accountInfo.Phone,
            Email = accountInfo.Email,
            DateOfBirth = accountInfo.DateOfBirth,
            Address = accountInfo.Address,
            Balance = accountInfo.Balance
        };
    }

    public List<TransactionHistoryItem> GetTransactionHistory(string AccountNumber)
    {
        List<TransactionHistoryRecord> ds = _acd.GetTransactionHistory(AccountNumber);
        List<TransactionHistoryItem> result = new();

        foreach (TransactionHistoryRecord gd in ds)
        {
            result.Add(new TransactionHistoryItem
            {
                AccountNumber = gd.AccountNumber,
                CounterAccount = gd.CounterAccount,
                TransactionType = gd.TransactionType,
                Amount = gd.Amount,
                BalanceBefore = gd.BalanceBefore,
                BalanceAfter = gd.BalanceAfter,
                TransactionDate = gd.TransactionDate,
                Direction = gd.TransactionType switch
                {
                    "TRANSFER_OUT" => "Chuyen di",
                    "TRANSFER_IN" => "Nhan ve",
                    "DEPOSIT" => "Nap tien",
                    "WITHDRAW" => "Rut tien",
                    _ => gd.TransactionType
                }
            });
        }

        _logger.LogInformation("Xem lich su - Xem lich su giao dich {AccountNumber} thanh cong", AccountNumber);
        return result;
    }
}
