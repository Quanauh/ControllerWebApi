namespace HuanBank.Api.Exceptions;

public class PhoneAlreadyExistsException : Exception
{
    public PhoneAlreadyExistsException(Exception? innerException = null)
        : base("Phone number already exists.", innerException)
    {
    }
}

public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException(Exception? innerException = null)
        : base("Email already exists.", innerException)
    {
    }
}

public class AccountAlreadyExistsException : Exception
{
    public AccountAlreadyExistsException(Exception? innerException = null)
        : base("Account number already exists.", innerException)
    {
    }
}
