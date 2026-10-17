using FluentValidation;

namespace Krizovka.Users.Application.Core.Backoffice.Commands.Login;

public class BackofficeLoginCommandValidator : AbstractValidator<BackofficeLoginCommand>
{
    public BackofficeLoginCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("E-mail je povinný.")
            .EmailAddress().WithMessage("E-mail nemá platný formát.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Heslo je povinné.");
    }
}
