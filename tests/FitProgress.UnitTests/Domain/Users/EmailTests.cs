using FitProgress.Domain.ValueObjects;

namespace FitProgress.UnitTests.Domain.Users;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("nao-e-email")]
    [InlineData("@example.com")]
    [InlineData("maria@")]
    public void TryCreate_FormatoInvalido_DeveRetornarErro(string valorInvalido)
    {
        var result = Email.TryCreate(valorInvalido, out var email, out var error);

        Assert.False(result);
        Assert.Null(email);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_EmailValido_DeveCriarComSucesso()
    {
        var result = Email.TryCreate("maria.silva@example.com", out var email, out var error);

        Assert.True(result);
        Assert.NotNull(email);
        Assert.Null(error);
        Assert.Equal("maria.silva@example.com", email!.Value);
    }

    [Fact]
    public void TryCreate_ComCapitalizacaoDiferente_DeveNormalizarParaLowercase()
    {
        var result = Email.TryCreate("Maria.Silva@Example.com", out var email, out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("maria.silva@example.com", email!.Value);
    }

    [Fact]
    public void TryCreate_ComEspacosNasPontas_DeveRemoverAoCriar()
    {
        var result = Email.TryCreate("  maria@example.com  ", out var email, out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("maria@example.com", email!.Value);
    }

    [Fact]
    public void Equals_DoisEmailsComCapitalizacaoDiferente_DevemSerIguais()
    {
        Email.TryCreate("Maria@Example.com", out var email1, out _);
        Email.TryCreate("maria@example.com", out var email2, out _);

        Assert.Equal(email1, email2);
        Assert.True(email1!.Equals(email2));
        Assert.Equal(email1.GetHashCode(), email2!.GetHashCode());
    }
}
