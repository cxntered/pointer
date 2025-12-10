namespace pointer.Core.Readers;

using System.Text.Json;
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
                .Select(file => new File(
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

    public IEnumerable<BeatmapCollection> GetCollections()
    {
        using var realm = Realm.GetInstance(config);
        var collections = realm.DynamicApi.All("BeatmapCollection");

        foreach (var collection in collections)
        {
            var name = collection.DynamicApi.Get<string>("Name");
            var hashes = collection.DynamicApi.GetList<string>("BeatmapMD5Hashes").ToList();

            yield return new BeatmapCollection(
                Name: name,
                Hashes: hashes
            );
        }
    }

    public IEnumerable<Skin> GetSkins()
    {
        using var realm = Realm.GetInstance(config);
        var skins = realm.DynamicApi.All("Skin");

        foreach (var skin in skins)
        {
            yield return new Skin(
                Name: skin.DynamicApi.Get<string>("Name"),
                IniName: null,
                InstantiationInfo: skin.DynamicApi.Get<string>("InstantiationInfo"),
                Files: [.. skin.DynamicApi.GetList<IRealmObjectBase>("Files")
                .Select(file => new File(
                    Filename: file.DynamicApi.Get<string>("Filename"),
                    Hash: file.DynamicApi.Get<IRealmObjectBase>("File").DynamicApi.Get<string>("Hash")
                ))]
            );
        }
    }

    public IEnumerable<Score> GetScores()
    {
        using var realm = Realm.GetInstance(config);
        var scores = realm.DynamicApi.All("Score");

        foreach (var score in scores)
        {
            var mods = score.DynamicApi.Get<string>("Mods");
            var files = score.DynamicApi.GetList<IRealmObjectBase>("Files")
                .Select(file => new File(
                    Filename: file.DynamicApi.Get<string>("Filename"),
                    Hash: file.DynamicApi.Get<IRealmObjectBase>("File").DynamicApi.Get<string>("Hash")
                ))
                .ToList();

            var beatmapInfo = score.DynamicApi.Get<IRealmObjectBase?>("BeatmapInfo");
            if (beatmapInfo == null) continue;

            string md5Hash = string.Empty;
            var replayFile = files.FirstOrDefault()!;
            string replayPath = Path.Combine(path, "files", replayFile.Hash[..1], replayFile.Hash[..2], replayFile.Hash);
            md5Hash = ReadReplayMD5Hash(replayPath) ?? score.DynamicApi.Get<string>("Hash");

            yield return new Score(
                BeatmapMD5Hash: beatmapInfo.DynamicApi.Get<string>("MD5Hash"),
                Ruleset: new Ruleset(
                    Name: score.DynamicApi.Get<IRealmObjectBase>("Ruleset").DynamicApi.Get<string>("Name"),
                    ID: score.DynamicApi.Get<IRealmObjectBase>("Ruleset").DynamicApi.Get<int>("OnlineID"),
                    ShortName: score.DynamicApi.Get<IRealmObjectBase>("Ruleset").DynamicApi.Get<string>("ShortName")
                ),
                User: new User(
                    Username: score.DynamicApi.Get<IRealmObjectBase>("User").DynamicApi.Get<string>("Username"),
                    ID: score.DynamicApi.Get<IRealmObjectBase>("User").DynamicApi.Get<int>("OnlineID"),
                    Country: score.DynamicApi.Get<IRealmObjectBase>("User").DynamicApi.Get<string>("CountryCode")
                ),
                MD5Hash: md5Hash,
                Date: score.DynamicApi.Get<DateTimeOffset>("Date"),
                TotalScore: score.DynamicApi.Get<int>("TotalScore"),
                MaxCombo: score.DynamicApi.Get<int>("MaxCombo"),
                Statistics: JsonSerializer.Deserialize<Statistics>(score.DynamicApi.Get<string>("Statistics"))!,
                MaximumStatistics: JsonSerializer.Deserialize<Statistics>(score.DynamicApi.Get<string>("MaximumStatistics"))!,
                Mods: !string.IsNullOrWhiteSpace(mods) ? JsonSerializer.Deserialize<List<Mod>>(mods)! : [],
                ID: score.DynamicApi.Get<int>("OnlineID"),
                IsLegacyScore: score.DynamicApi.Get<bool>("IsLegacyScore"),
                Files: files,
                StableScore: null
            );
        }
    }

    private static string? ReadReplayMD5Hash(string replayPath)
    {
        if (!System.IO.File.Exists(replayPath))
            return null;

        using var stream = System.IO.File.OpenRead(replayPath);
        using var reader = new BinaryReader(stream);

        reader.ReadByte(); // ruleset
        reader.ReadInt32(); // version
        StableDatabaseReader.ReadString(reader); // beatmap hash
        StableDatabaseReader.ReadString(reader); // player name
        return StableDatabaseReader.ReadString(reader); // replay hash
    }
}