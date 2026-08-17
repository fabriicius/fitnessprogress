using FitProgress.Domain.ValueObjects;

namespace FitProgress.UnitTests.Domain.Users;

public class PersonNameTests
{
    [Fact]
    public void TryCreate_NomeVazio_DeveRetornarErro()
    {
        var result = PersonName.TryCreate(string.Empty, out var personName, out var error);

        Assert.False(result);
        Assert.Null(personName);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_ApenasEspacos_DeveRetornarErro()
    {
        var result = PersonName.TryCreate("   ", out var personName, out var error);

        Assert.False(result);
        Assert.Null(personName);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_AcimaDoLimiteMaximo_DeveRetornarErro()
    {
        var nomeMuitoLongo = new string('a', PersonName.MaxLength + 1);

        var result = PersonName.TryCreate(nomeMuitoLongo, out var personName, out var error);

        Assert.False(result);
        Assert.Null(personName);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_NoLimiteMaximo_DeveCriarComSucesso()
    {
        var nomeNoLimite = new string('a', PersonName.MaxLength);

        var result = PersonName.TryCreate(nomeNoLimite, out var personName, out var error);

        Assert.True(result);
        Assert.NotNull(personName);
        Assert.Null(error);
        Assert.Equal(nomeNoLimite, personName!.Value);
    }

    [Fact]
    public void TryCreate_ComEspacosNasPontas_DeveRemoverAoCriar()
    {
        var result = PersonName.TryCreate("  Maria Silva  ", out var personName, out var error);

        Assert.True(result);
        Assert.Null(error);
        Assert.Equal("Maria Silva", personName!.Value);
    }

    [Fact]
    public void TryCreate_NomeValido_DeveCriarComSucesso()
    {
        var result = PersonName.TryCreate("Maria Silva", out var personName, out var error);

        Assert.True(result);
        Assert.NotNull(personName);
        Assert.Null(error);
        Assert.Equal("Maria Silva", personName!.Value);
    }
}
