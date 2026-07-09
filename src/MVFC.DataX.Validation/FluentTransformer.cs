namespace MVFC.DataX.Validation;

public sealed class FluentTransformer<TInput, TOutput>(Func<TInput, TOutput?> mapFunc, IValidator<TOutput> validator) : IDataTransformer<TInput, TOutput>
{
    private readonly Func<TInput, TOutput?> _mapFunc = mapFunc ?? throw new ArgumentNullException(nameof(mapFunc));
    private readonly IValidator<TOutput> _validator = validator ?? throw new ArgumentNullException(nameof(validator));

    public async IAsyncEnumerable<DataResult<TOutput>> TransformAsync(
        IAsyncEnumerable<TInput> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            var mapped = _mapFunc(item);
            if (mapped is null)
            {
                yield return DataResult.Failure<TOutput>([new DataError("Mapping", "Mapping returned null", item)]);
                continue;
            }

            var result = await _validator.ValidateAsync(mapped, ct).ConfigureAwait(false);
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(e => new DataError(e.PropertyName, e.ErrorMessage, e.AttemptedValue));
                yield return DataResult.Failure<TOutput>(errors);
                continue;
            }

            yield return DataResult.Success(mapped);
        }
    }
}
