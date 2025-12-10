namespace pointer.Core.Models;

public record Skin(
    string Name,
    string? IniName,
    string InstantiationInfo,
    List<File> Files
);
