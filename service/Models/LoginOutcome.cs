using HuanBank.Api.Result;

namespace HuanBank.Api.Services.Models;

public sealed record LoginOutcome(LoginResult Result, string? Role = null);
