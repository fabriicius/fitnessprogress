using System.Net.Mail;

namespace FitProgress.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public const int MaxLength = 320;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string? rawValue, out Email? email, out string? error)
    {
        var trimmed = rawValue?.Trim() ?? string.Empty;

        if (trimmed.Length == 0 || trimmed.Length > MaxLength || !IsValidFormat(trimmed))
        {
            email = null;
            error = "O e-mail informado é inválido.";
            return false;
        }

        email = new Email(trimmed.ToLowerInvariant());
        error = null;
        return true;
    }

    private static bool IsValidFormat(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return address.Address == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
