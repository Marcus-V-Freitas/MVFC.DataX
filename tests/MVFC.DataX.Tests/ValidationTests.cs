namespace MVFC.DataX.Tests;

public sealed class ValidationTests
{
    private readonly BankInfoValidator _validator = new();

    [Fact]
    public void BankInfoValidator_Deve_rejeitar_nome_vazio()
    {
        // Arrange
        var info = new BankInfo { Ispb = 1, Code = 1, Name = "" };

        // Act
        var result = _validator.Validate(info);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void BankInfoValidator_Deve_rejeitar_codigo_zero_ou_negativo()
    {
        // Arrange
        var info = new BankInfo { Ispb = 1, Code = 0, Name = "Test" };

        // Act
        var result = _validator.Validate(info);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void BankInfoValidator_Deve_aceitar_banco_valido()
    {
        // Arrange
        var info = new BankInfo { Ispb = 1, Code = 1, Name = "Test" };

        // Act
        var result = _validator.Validate(info);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FluentTransformer_Deve_retornar_falha_para_banco_sem_ispb()
    {
        // Arrange
        var transformer = new FluentTransformer<Bank, BankInfo>(TestHelpers.MapBank, _validator);
        var input = CreateAsyncEnumerable(new Bank(null, "Test", 1, "Test"));

        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().ContainSingle().Which.IsFailure.Should().BeTrue();
        results[0].Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Mapping returned null");
    }

    [Fact]
    public async Task FluentTransformer_Deve_transformar_banco_valido()
    {
        // Arrange
        var transformer = new FluentTransformer<Bank, BankInfo>(TestHelpers.MapBank, _validator);
        var input = CreateAsyncEnumerable(new Bank(123, "Test", 1, "Test Full"));

        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().ContainSingle().Which.IsSuccess.Should().BeTrue();
        results[0].Value!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task FluentTransformer_Deve_retornar_falha_para_nome_vazio()
    {
        // Arrange
        var transformer = new FluentTransformer<Bank, BankInfo>(TestHelpers.MapBank, _validator);
        var input = CreateAsyncEnumerable(new Bank(123, "", 1, ""));

        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().ContainSingle().Which.IsFailure.Should().BeTrue();
    }
}
