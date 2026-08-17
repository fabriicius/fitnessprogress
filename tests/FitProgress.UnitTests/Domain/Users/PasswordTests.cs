using FitProgress.Domain.ValueObjects;

namespace FitProgress.UnitTests.Domain.Users;

public class PasswordTests
{
    [Fact]
    public void TryCreate_MenorQueOMinimo_DeveRetornarErro()
    {
        var result = Password.TryCreate("Ab1", out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_SemLetraMaiuscula_DeveRetornarErro()
    {
        var result = Password.TryCreate("senha123", out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_SemLetraMinuscula_DeveRetornarErro()
    {
        var result = Password.TryCreate("SENHA123", out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_SemDigito_DeveRetornarErro()
    {
        var result = Password.TryCreate("SenhaForte", out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_AcimaDoLimiteMaximo_DeveRetornarErro()
    {
        var senhaGigante = "Senha1" + new string('a', Password.MaxLength);

        var result = Password.TryCreate(senhaGigante, out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_NoLimiteMaximo_DeveCriarComSucesso()
    {
        var senhaNoLimite = "Senha1" + new string('a', Password.MaxLength - 6);

        var result = Password.TryCreate(senhaNoLimite, out var password, out var error);

        Assert.True(result);
        Assert.NotNull(password);
        Assert.Null(error);
        Assert.Equal(Password.MaxLength, senhaNoLimite.Length);
    }

    [Fact]
    public void TryCreate_UmCaractereAcimaDoLimiteMaximo_DeveRetornarErro()
    {
        var senhaAcimaDoLimite = "Senha1" + new string('a', Password.MaxLength - 5);

        var result = Password.TryCreate(senhaAcimaDoLimite, out var password, out var error);

        Assert.False(result);
        Assert.Null(password);
        Assert.NotNull(error);
        Assert.Equal(Password.MaxLength + 1, senhaAcimaDoLimite.Length);
    }

    [Fact]
    public void TryCreate_SenhaValida_DeveCriarComSucesso()
    {
        var result = Password.TryCreate("Senha123", out var password, out var error);

        Assert.True(result);
        Assert.NotNull(password);
        Assert.Null(error);
        Assert.Equal("Senha123", password!.Value);
    }
}
