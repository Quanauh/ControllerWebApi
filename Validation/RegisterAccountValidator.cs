using System.Globalization;
using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;
namespace HuanBank.Api.Validators;
public sealed class RegisterAccountValidator : IRegisterAccountValidator
{
    public RegistrationValidationResult Validate(RegisterAccountCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)
            || string.IsNullOrWhiteSpace(command.Phone)
            || string.IsNullOrWhiteSpace(command.Email)
            || string.IsNullOrWhiteSpace(command.DateOfBirth)
            || string.IsNullOrWhiteSpace(command.AccountNumber)
            || string.IsNullOrWhiteSpace(command.Password))
        {
            return RegistrationValidationResult.MissingRequiredField;
        }

        return DateTime.TryParseExact(
            command.DateOfBirth,
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _)
            ? RegistrationValidationResult.Success
            : RegistrationValidationResult.InvalidDate;
    }
}
