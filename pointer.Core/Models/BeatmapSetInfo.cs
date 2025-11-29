namespace Pointer.Core.Models;

using pointer.Core.Models;

public record BeatmapSetInfo(
    int? OnlineID,
    string Title,
    string Artist,
    string Creator,
    List<BeatmapInfo> Beatmaps,
    List<BeatmapFileInfo> Files
);
