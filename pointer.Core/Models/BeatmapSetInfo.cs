using Realms;

namespace pointer.Core.Models;

public record BeatmapSetInfo(
    int? OnlineID,
    string Title,
    string Artist,
    string Creator,
    bool Protected,
    List<BeatmapInfo> Beatmaps,
    List<File> Files
)
{
    public static BeatmapSetInfo FromDynamic(IRealmObject beatmapSet)
    {
        var beatmaps = beatmapSet.DynamicApi.GetList<IRealmObjectBase>("Beatmaps")
            .Select(BeatmapInfo.FromDynamic)
            .ToList();
        var firstBeatmap = beatmaps.FirstOrDefault();

        return new BeatmapSetInfo(
            OnlineID: beatmapSet.DynamicApi.Get<int?>("OnlineID"),
            Title: firstBeatmap?.Title ?? string.Empty,
            Artist: firstBeatmap?.Artist ?? string.Empty,
            Creator: firstBeatmap?.Creator ?? string.Empty,
            Protected: beatmapSet.DynamicApi.Get<bool>("Protected"),
            Beatmaps: beatmaps,
            Files: beatmapSet.DynamicApi.GetList<IRealmObjectBase>("Files")
                .Select(File.FromDynamic)
                .ToList()
        );
    }
}
