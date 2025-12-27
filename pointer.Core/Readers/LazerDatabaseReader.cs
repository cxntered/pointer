namespace pointer.Core.Readers;

using pointer.Core.Models;
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
        return [.. realm.DynamicApi.All("BeatmapSet")
            .AsEnumerable()
            .Select(BeatmapSetInfo.FromDynamic)];
    }

    public IEnumerable<BeatmapCollection> GetCollections()
    {
        using var realm = Realm.GetInstance(config);
        return [.. realm.DynamicApi.All("BeatmapCollection")
            .AsEnumerable()
            .Select(BeatmapCollection.FromDynamic)];
    }

    public IEnumerable<Skin> GetSkins()
    {
        using var realm = Realm.GetInstance(config);
        return [.. realm.DynamicApi.All("Skin")
            .AsEnumerable()
            .Select(Skin.FromDynamic)];
    }

    public IEnumerable<Score> GetScores()
    {
        using var realm = Realm.GetInstance(config);
        return [.. realm.DynamicApi.All("Score")
            .AsEnumerable()
            .Where(score => score.DynamicApi.Get<IRealmObjectBase?>("BeatmapInfo") != null)
            .Select(score => Score.FromDynamic(score, path))];
    }
}