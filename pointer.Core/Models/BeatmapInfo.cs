namespace pointer.Core.Models;

public record BeatmapInfo(
    string Hash,
    string? FolderName,
    string Title,
    string Artist,
    string Creator,
    string Difficulty
);
