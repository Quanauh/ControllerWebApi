namespace HuanBank.Api.Result;

public enum RegistrationResult
{
    Success,
    MissingRequiredField,
    InvalidDate,
    AccountAlreadyExists,
    PhoneAlreadyExists,
    EmailAlreadyExists
}

public enum LoginResult
{
    Success,
    AccountNotFound,
    IncorrectPassword,
    AccountInactive,
    InvalidRole
}
