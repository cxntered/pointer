namespace pointer.Core.Models;

public record Score(
    string BeatmapHash,
    DateTimeOffset Date,
    List<File> Files
);
