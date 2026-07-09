namespace MVFC.DataX.Tests.Contracts;

public interface IBrasilApi
{
    [Get("/banks/v1")]
    public Task<IEnumerable<Bank>> GetBanksAsync(CancellationToken ct);
}
