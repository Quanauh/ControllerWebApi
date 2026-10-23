using Microsoft.AspNetCore.Mvc;
using HuanBank.Api.Services;
using HuanBank.Api.Model.Request;
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
                LoginResult.Success =>
                    Ok(new { message = "Dang nhap thanh cong", token = tokenService.GenerateToken(req.stk) }),
                LoginResult.AccountNotFound =>
                    NotFound(new { message = "Khong tim thay tai khoan" }),
                LoginResult.IncorrectPassword =>
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
                RegistrationResult.Success =>
                    Created($"/account/{req.AccountNumber}", new { message = "Tao tai khoan thanh cong", stk = req.AccountNumber }),
                RegistrationResult.MissingRequiredField =>
                    BadRequest(new { message = "Khong duoc de trong thong tin" }),
                RegistrationResult.AccountAlreadyExists =>
                    Conflict(new { message = "So tai khoan da ton tai" }),
                RegistrationResult.PhoneAlreadyExists =>
                    Conflict(new { message = "So dien thoai da ton tai" }),
                RegistrationResult.EmailAlreadyExists =>
                    Conflict(new { message = "Email da ton tai" }),
                RegistrationResult.InvalidDate =>
                    BadRequest(new { message = "Ngay sinh khong hop le" }),
                _ => StatusCode(500, new { message = "Loi he thong" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tao tai khoan - Loi he thong khi tao STK {AccountNumber}", req.AccountNumber);
            return StatusCode(500, new { message = "Loi he thong" });
        }
    }
}

// public record LoginRequest(string stk,string mk);
