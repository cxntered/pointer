using Realms;

namespace pointer.Core.Models;

public record BeatmapInfo(
    string Hash,
    string? FolderName,
    string Title,
    string Artist,
    string Creator,
    string Difficulty
)
{
    public static BeatmapInfo FromDynamic(IRealmObjectBase beatmap)
    {
        var metadata = beatmap.DynamicApi.Get<IRealmObjectBase>("Metadata");
        return new BeatmapInfo(
            Hash: beatmap.DynamicApi.Get<string>("MD5Hash"),
            FolderName: null,
            Title: metadata.DynamicApi.Get<string>("Title"),
            Artist: metadata.DynamicApi.Get<string>("Artist"),
            Creator: metadata.DynamicApi.Get<IRealmObjectBase>("Author").DynamicApi.Get<string>("Username"),
            Difficulty: beatmap.DynamicApi.Get<string>("DifficultyName")
        );
    }
}
