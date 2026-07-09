namespace MVFC.DataX.Tests.Models;

public sealed record BankInfo
{
    public int Ispb { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Code { get; init; }
}
