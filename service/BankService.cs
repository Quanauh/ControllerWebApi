using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using HuanBank.Api.DAO;
using HuanBank.Api.DAO.Models;
using HuanBank.Api.Validators;
using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;
using HuanBank.Api.Exceptions;

namespace HuanBank.Api.Services;

public class BankService : IBankService
{
    private readonly IAccountDAO _acd;
    private readonly ILogger<BankService> _logger;
    private readonly IRegisterAccountValidator _registrationValidator;
    private readonly IMoneyTransactionValidator _transactionValidator;

    public BankService(
        IAccountDAO acd,
        ILogger<BankService> logger,
        IRegisterAccountValidator registrationValidator,
        IMoneyTransactionValidator transactionValidator)
    {
        _acd = acd;
        _logger = logger;
        _registrationValidator = registrationValidator;
        _transactionValidator = transactionValidator;
    }

    public bool AccountExists(string AccountNumber)
    {
        return _acd.Exists(AccountNumber);
    }

    // Dang ky
    public RegistrationResult CreateAccount(RegisterAccountCommand command)
    {
        var validation = _registrationValidator.Validate(command);
        if (validation != RegistrationValidationResult.Success)
        {
            _logger.LogWarning("Dang ky - Validate that bai: {Result}", validation);
            return validation switch
            {
                RegistrationValidationResult.MissingRequiredField => RegistrationResult.MissingRequiredField,
                RegistrationValidationResult.InvalidDate => RegistrationResult.InvalidDate,
                _ => throw new ArgumentOutOfRangeException(nameof(validation))
            };
        }

        if (_acd.Exists(command.AccountNumber))
        {
            _logger.LogWarning("Dang Ki - Sotk {AccountNumber} da ton tai", command.AccountNumber);
            return RegistrationResult.AccountAlreadyExists;
        }

        try
        {
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
        }
        catch (AccountAlreadyExistsException)
        {
            _logger.LogWarning("Dang ky that bai: tai khoan {AccountNumber} da ton tai", command.AccountNumber);
            return RegistrationResult.AccountAlreadyExists;
        }
        catch (PhoneAlreadyExistsException)
        {
            _logger.LogWarning("Dang ky that bai: so dien thoai {Phone} da ton tai", command.Phone);
            return RegistrationResult.PhoneAlreadyExists;
        }
        catch (EmailAlreadyExistsException)
        {
            _logger.LogWarning("Dang ky that bai: email {Email} da ton tai", command.Email);
            return RegistrationResult.EmailAlreadyExists;
        }

        _logger.LogInformation("Tao Tai Khoan - Tao tai khoan {AccountNumber} thanh cong", command.AccountNumber);
        return RegistrationResult.Success;
    }

    // Dang nhap
    public LoginResult Login(string AccountNumber, string Password)
    {
        if (!AccountExists(AccountNumber))
        {
            _logger.LogWarning("So tai khoan {AccountNumber} khong ton tai", AccountNumber);
            return LoginResult.AccountNotFound;
        }

        if (!_acd.checkPassWord(AccountNumber, Password))
        {
            _logger.LogWarning("Dang Nhap - So tai khoan {AccountNumber} nhap khong dung mat khau", AccountNumber);
            return LoginResult.IncorrectPassword;
        }

        _logger.LogInformation("Dang Nhap - Stk {AccountNumber} dang nhap thanh cong", AccountNumber);
        return LoginResult.Success;
    }

    // Nap tien
    public TransactionResult Deposit(string AccountNumber, decimal a)
    {
        if (!AccountExists(AccountNumber))
        {
            _logger.LogWarning("Nap tien - So tai khoan {AccountNumber} khong ton tai, nap tien that bai", AccountNumber);
            return TransactionResult.AccountNotFound;
        }

        var validation = _transactionValidator.ValidateAmount(a);
        if (validation != MoneyTransactionValidationResult.Success)
        {
            _logger.LogWarning("Nap tien - So tai khoan {AccountNumber} nap tien that bai, so tien {Amount} khong hop le", AccountNumber, a);
            return TransactionResult.InvalidAmount;
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

        var validation = _transactionValidator.ValidateAmount(a);
        if (validation != MoneyTransactionValidationResult.Success)
        {
            _logger.LogWarning("Rut tien - So tai khoan {AccountNumber} rut tien that bai, ket qua validate: {Result}", AccountNumber, validation);
            return TransactionResult.InvalidAmount;
        }

        if (!_acd.TryWithdraw(AccountNumber, a))
        {
            _logger.LogWarning("Rut tien - So tai khoan {AccountNumber} khong du so du", AccountNumber);
            return TransactionResult.InsufficientBalance;
        }
        _logger.LogInformation("Rut tien - So tai khoan {AccountNumber} rut tien thanh cong", AccountNumber);
        return TransactionResult.Success;
    }

    // Chuyen tien
    public TransactionResult TransferMoney(string AccountNumber1, string AccountNumber2, decimal a)
    {
        var validation = _transactionValidator.ValidateTransfer(AccountNumber1, AccountNumber2, a);
        if (validation != MoneyTransactionValidationResult.Success)
        {
            return validation switch
            {
                MoneyTransactionValidationResult.InvalidAmount => TransactionResult.InvalidAmount,
                MoneyTransactionValidationResult.CannotTransferToSelf => TransactionResult.CannotTransferToSelf,
                _ => throw new ArgumentOutOfRangeException(nameof(validation))
            };
        }

        if (!AccountExists(AccountNumber1))
        {
            _logger.LogWarning("Chuyen tien - So tai khoan gui {AccountNumber1} khong ton tai", AccountNumber1);
            return TransactionResult.AccountNotFound;
        }

        if (!AccountExists(AccountNumber2))
        {
            _logger.LogWarning("Chuyen tien - So tai khoan {AccountNumber2} khong ton tai, chuyen tien that bai", AccountNumber2);
            return TransactionResult.AccountNotFound;
        }

        if (!_acd.TryTransferMoney(AccountNumber1, AccountNumber2, a))
        {
            _logger.LogWarning("Chuyen tien - So tai khoan {AccountNumber1} khong du so du", AccountNumber1);
            return TransactionResult.InsufficientBalance;
        }

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
