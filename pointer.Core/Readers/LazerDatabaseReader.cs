namespace pointer.Core.Readers;

using pointer.Core.Models;
using Realms;

public class LazerDatabaseReader(string databasePath)
{
    private readonly string databasePath = databasePath;

    public IEnumerable<BeatmapInfo> GetBeatmaps()
    {
        var config = new RealmConfiguration(databasePath)
        {
            SchemaVersion = 51,
            IsReadOnly = true,
            IsDynamic = true
        };

        using var realm = Realm.GetInstance(config);
        var beatmaps = realm.DynamicApi.All("Beatmap");
        var metadata = realm.DynamicApi.All("BeatmapMetadata");

        foreach (var beatmap in beatmaps)
        {
            yield return new BeatmapInfo(
                Hash: beatmap.DynamicApi.Get<string>("MD5Hash"),
                FolderName: null,
                Title: beatmap.DynamicApi.Get<IRealmObjectBase>("Metadata").DynamicApi.Get<string>("Title"),
                Artist: beatmap.DynamicApi.Get<IRealmObjectBase>("Metadata").DynamicApi.Get<string>("Artist"),
                Creator: beatmap.DynamicApi.Get<IRealmObjectBase>("Metadata").DynamicApi.Get<IRealmObjectBase>("Author").DynamicApi.Get<string>("Username"),
                Difficulty: beatmap.DynamicApi.Get<string>("DifficultyName")
            );
        }
    }
}