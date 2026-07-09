namespace MVFC.DataX.Tests.Helpers;

public static class TestHelpers
{
    public static BankInfo? MapBank(Bank bank) =>
        bank?.Ispb is null
            ? null
            : new BankInfo
            {
                Ispb = bank.Ispb.Value,
                Name = bank.Name ?? string.Empty,
                Code = bank.Code ?? 0
            };
}
