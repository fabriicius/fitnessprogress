namespace FitProgress.Domain.ValueObjects;

public sealed class PersonName : IEquatable<PersonName>
{
    public const int MaxLength = 200;

    public string Value { get; }

    private PersonName(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? rawValue, out PersonName? personName, out string? error)
    {
        var trimmed = rawValue?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            personName = null;
            error = "O nome é obrigatório.";
            return false;
        }

        if (trimmed.Length > MaxLength)
        {
            personName = null;
            error = $"O nome deve ter no máximo {MaxLength} caracteres.";
            return false;
        }

        personName = new PersonName(trimmed);
        error = null;
        return true;
    }

    public bool Equals(PersonName? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as PersonName);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
