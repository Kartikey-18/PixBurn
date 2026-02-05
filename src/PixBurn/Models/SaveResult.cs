namespace PixBurn.Models;

public class SaveResult
{
    public string SourceFile { get; init; } = string.Empty;
    public string OutputFile { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
