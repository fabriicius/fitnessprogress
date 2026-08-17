namespace FitProgress.Domain.ValueObjects;

public sealed class Password
{
    public const int MinLength = 8;
    public const int MaxLength = 100;

    public string Value { get; }

    private Password(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? rawValue, out Password? password, out string? error)
    {
        var value = rawValue ?? string.Empty;

        if (value.Length < MinLength || value.Length > MaxLength)
        {
            password = null;
            error = $"A senha deve ter entre {MinLength} e {MaxLength} caracteres.";
            return false;
        }

        if (!value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit))
        {
            password = null;
            error = "A senha deve conter ao menos uma letra maiúscula, uma minúscula e um número.";
            return false;
        }

        password = new Password(value);
        error = null;
        return true;
    }
}
