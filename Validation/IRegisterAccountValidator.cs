using HuanBank.Api.Services.Models;
using HuanBank.Api.Result;
namespace HuanBank.Api.Validators;
public interface IRegisterAccountValidator
{
    RegistrationValidationResult Validate(RegisterAccountCommand command);
}
