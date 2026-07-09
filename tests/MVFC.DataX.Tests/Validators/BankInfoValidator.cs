namespace MVFC.DataX.Tests.Validators;

public sealed class BankInfoValidator : AbstractValidator<BankInfo>
{
    public BankInfoValidator()
    {
        RuleFor(b => b.Name).NotEmpty().MaximumLength(200);
        RuleFor(b => b.Code).GreaterThan(0);
        RuleFor(b => b.Ispb).NotNull();
    }
}
