namespace pointer.Core.Models;

public record BeatmapSetInfo(
    int? OnlineID,
    string Title,
    string Artist,
    string Creator,
    bool Protected,
    List<BeatmapInfo> Beatmaps,
    List<File> Files
);
