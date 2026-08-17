namespace FitProgress.Domain.ValueObjects;

public sealed class PasswordHash
{
    public string Value { get; }

    private PasswordHash(string value)
    {
        Value = value;
    }

    public static PasswordHash FromHashedValue(string hashedValue)
    {
        if (string.IsNullOrWhiteSpace(hashedValue))
        {
            throw new ArgumentException("O hash da senha não pode ser vazio.", nameof(hashedValue));
        }

        return new PasswordHash(hashedValue);
    }
}
