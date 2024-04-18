using FastEndpoints;
using FluentValidation;

namespace Author.Signup;

internal sealed class Request
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

internal sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("your name is required!")
            .MinimumLength(3).WithMessage("name is too short!")
            .MaximumLength(25).WithMessage("name is too long!");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("email address is required!")
            .EmailAddress().WithMessage("the format of your email address is wrong!");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("a username is required!")
            .MinimumLength(3).WithMessage("username is too short!")
            .MaximumLength(15).WithMessage("username is too long!");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("a password is required!")
            .MinimumLength(10).WithMessage("password is too short!")
            .MaximumLength(25).WithMessage("password is too long!");
    }
}

internal sealed class Response
{
    public string Message { get; set; } = string.Empty;
}
