namespace FitProgress.Application.Results;

public sealed record ValidationError(string Field, string Message);
