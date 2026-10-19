using Microsoft.AspNetCore.Mvc;
using HuanBank.Api.Services;
using HuanBank.Api.Model.Request;
using HuanBank.Api.Exceptions;
using HuanBank.Api.Result;
using HuanBank.Api.Services.Models;
using HuanBank.Api.Util;
namespace HuanBank.Api.Controller;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IBankService _qlnh;
    private readonly TokenService tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IBankService _qlnh, TokenService tokenService, ILogger<AuthController> logger)
    {
        this._qlnh = _qlnh;
        this.tokenService = tokenService;
        _logger = logger;
    }
    [HttpPost("Login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        try
        {
            var kq = _qlnh.Login(req.stk, req.mk);
            return kq switch
            {
                TransactionResult.Success =>
                    Ok(new { message = "Dang nhap thanh cong", token = tokenService.GenerateToken(req.stk) }),
                TransactionResult.AccountNotFound =>
                    NotFound(new { message = "Khong tim thay tai khoan" }),
                TransactionResult.IncorrectPassword =>
                    BadRequest(new { message = "Sai mat khau" }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dang nhap - Loi he thong khi STK {AccountNumber} dang nhap", req.stk);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
    [HttpPost("Register")]
    public IActionResult Register([FromBody] RegisterRequest req)
    {
        try
        {
            var command = new RegisterAccountCommand
            {
                Name = req.Name,
                Phone = req.Phone,
                Email = req.Email,
                DateOfBirth = req.DateOfBirth,
                Address = req.Address,
                AccountNumber = req.AccountNumber,
                Password = req.Password
            };

            var kq = _qlnh.CreateAccount(command);
            return kq switch
            {
                TransactionResult.Success =>
                    Created($"/account/{req.AccountNumber}", new { message = "Tao tai khoan thanh cong", stk = req.AccountNumber }),
                TransactionResult.IsNull =>
                    BadRequest(new { message = "Khong duoc de trong thong tin" }),
                TransactionResult.AccountAlreadyExists =>
                    Conflict(new { message = "So tai khoan da ton tai" }),
                TransactionResult.InvalidDate =>
                    BadRequest(new { message = "Ngay sinh khong hop le" }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (AccountAlreadyExistsException)
        {
            _logger.LogWarning("Tao tai khoan - So tai khoan {AccountNumber} da ton tai", req.AccountNumber);
            return Conflict(new { message = "So tai khoan da ton tai" });
        }
        catch (PhoneAlreadyExistsException)
        {
            _logger.LogWarning("Tao tai khoan - So dien thoai {Phone} da ton tai", req.Phone);
            return Conflict(new { message = "So dien thoai da ton tai" });
        }
        catch (EmailAlreadyExistsException)
        {
            _logger.LogWarning("Tao tai khoan - Email {Email} da ton tai", req.Email);
            return Conflict(new { message = "Email da ton tai" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tao tai khoan - Loi he thong khi tao STK {AccountNumber}", req.AccountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
}

// public record LoginRequest(string stk,string mk);
