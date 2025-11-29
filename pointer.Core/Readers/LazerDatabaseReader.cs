namespace pointer.Core.Readers;

using pointer.Core.Models;
using Pointer.Core.Models;
using Realms;

public class LazerDatabaseReader(string path)
{
    private readonly RealmConfiguration config = new(Path.Combine(path, "client.realm"))
    {
        SchemaVersion = 51,
        IsReadOnly = true,
        IsDynamic = true
    };

    public IEnumerable<BeatmapSetInfo> GetBeatmapSets()
    {
        using var realm = Realm.GetInstance(config);
        var beatmapSets = realm.DynamicApi.All("BeatmapSet");

        foreach (var beatmapSet in beatmapSets)
        {
            var beatmaps = beatmapSet.DynamicApi.GetList<IRealmObjectBase>("Beatmaps")
                .Select(beatmap =>
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
                    })
                .ToList();

            var files = beatmapSet.DynamicApi.GetList<IRealmObjectBase>("Files")
                .Select(file => new BeatmapFileInfo(
                    Filename: file.DynamicApi.Get<string>("Filename"),
                    Hash: file.DynamicApi.Get<IRealmObjectBase>("File").DynamicApi.Get<string>("Hash")
                ))
                .ToList();

            var firstBeatmap = beatmapSet.DynamicApi.GetList<IRealmObjectBase>("Beatmaps").FirstOrDefault();
            var metadata = firstBeatmap?.DynamicApi.Get<IRealmObjectBase>("Metadata");

            yield return new BeatmapSetInfo(
                OnlineID: beatmapSet.DynamicApi.Get<int?>("OnlineID"),
                Title: metadata?.DynamicApi.Get<string>("Title") ?? string.Empty,
                Artist: metadata?.DynamicApi.Get<string>("Artist") ?? string.Empty,
                Creator: metadata?.DynamicApi.Get<IRealmObjectBase>("Author")?.DynamicApi.Get<string>("Username") ?? string.Empty,
                Protected: beatmapSet.DynamicApi.Get<bool>("Protected"),
                Beatmaps: beatmaps,
                Files: files
            );
        }
    }

    public IEnumerable<BeatmapInfo> GetBeatmaps()
    {
        using var realm = Realm.GetInstance(config);
        var beatmaps = realm.DynamicApi.All("Beatmap");

        foreach (var beatmap in beatmaps)
        {
            var metadata = beatmap.DynamicApi.Get<IRealmObjectBase>("Metadata");
            yield return new BeatmapInfo(
                Hash: beatmap.DynamicApi.Get<string>("MD5Hash"),
                FolderName: null,
                Title: metadata.DynamicApi.Get<string>("Title"),
                Artist: metadata.DynamicApi.Get<string>("Artist"),
                Creator: metadata.DynamicApi.Get<IRealmObjectBase>("Author").DynamicApi.Get<string>("Username"),
                Difficulty: beatmap.DynamicApi.Get<string>("DifficultyName")
            );
        }
    }
}