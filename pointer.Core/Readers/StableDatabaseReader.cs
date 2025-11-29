namespace pointer.Core.Readers;

using pointer.Core.Models;

public class StableDatabaseReader(string path)
{
    private readonly string osuDbPath = Path.Combine(path, "osu!.db");
    private readonly string collectionDbPath = Path.Combine(path, "collection.db");
    private readonly string scoresDbPath = Path.Combine(path, "scores.db");

    public IEnumerable<BeatmapInfo> GetBeatmaps()
    {
        using var stream = System.IO.File.OpenRead(osuDbPath);
        using var reader = new BinaryReader(stream);

        int version = reader.ReadInt32(); // version
        reader.ReadInt32(); // folder count
        reader.ReadBoolean(); // account unlocked
        reader.ReadInt64(); // account unlock time
        ReadString(reader); // account name

        int beatmapCount = reader.ReadInt32();

        for (int i = 0; i < beatmapCount; i++)
        {
            if (version < 20191106)
                reader.ReadInt32();

            string artist = ReadString(reader);
            ReadString(reader); // artist unicode
            string title = ReadString(reader);
            ReadString(reader); // title unicode
            string creator = ReadString(reader);
            string difficulty = ReadString(reader);
            ReadString(reader); // audio file name
            string hash = ReadString(reader);
            ReadString(reader); // osu file name
            reader.ReadByte(); // ranked status
            reader.ReadInt16(); // hit circle count
            reader.ReadInt16(); // slider count
            reader.ReadInt16(); // spinner count
            reader.ReadInt64(); // last modified

            if (version < 20140609)
            {
                reader.ReadByte(); // approach rate
                reader.ReadByte(); // circle size
                reader.ReadByte(); // hp drain
                reader.ReadByte(); // overall difficulty
            }
            else
            {
                reader.ReadSingle(); // approach rate
                reader.ReadSingle(); // circle size
                reader.ReadSingle(); // hp drain
                reader.ReadSingle(); // overall difficulty
            }

            reader.ReadDouble();

            if (version >= 20140609)
            {
                ReadStarRatings(reader, version); // standard star rating
                ReadStarRatings(reader, version); // taiko star rating
                ReadStarRatings(reader, version); // catch star rating
                ReadStarRatings(reader, version); // mania star rating
            }

            reader.ReadInt32(); // drain time
            reader.ReadInt32(); // total time
            reader.ReadInt32(); // preview time

            int timingPointsCount = reader.ReadInt32();
            for (int j = 0; j < timingPointsCount; j++)
            {
                reader.ReadDouble(); // bpm
                reader.ReadDouble(); // offset
                reader.ReadByte();   // is inherited
            }

            reader.ReadInt32(); // difficulty id
            reader.ReadInt32(); // beatmap id
            reader.ReadInt32(); // thread id
            reader.ReadByte(); // standard grade
            reader.ReadByte(); // taiko grade
            reader.ReadByte(); // catch grade
            reader.ReadByte(); // mania grade
            reader.ReadInt16(); // local offset
            reader.ReadSingle(); // stack leniency
            reader.ReadByte(); // gamemode
            ReadString(reader); // song source
            ReadString(reader); // song tags
            reader.ReadInt16(); // online offset
            ReadString(reader); // title font name
            reader.ReadBoolean(); // is unplayed
            reader.ReadInt64(); // last played
            reader.ReadBoolean(); // is osz2
            string folderName = ReadString(reader);
            reader.ReadInt64(); // last checked
            reader.ReadBoolean(); // ignore beatmap sound
            reader.ReadBoolean(); // ignore beatmap skin
            reader.ReadBoolean(); // disable storyboard
            reader.ReadBoolean(); // disable video
            reader.ReadBoolean(); // visual override

            if (version < 20140609)
                reader.ReadInt16(); // unknown short

            reader.ReadInt32(); // last modification time
            reader.ReadByte(); // mania scroll speed

            yield return new BeatmapInfo(
                Hash: hash,
                FolderName: folderName,
                Title: title,
                Artist: artist,
                Creator: creator,
                Difficulty: difficulty
            );
        }
    }

    public IEnumerable<BeatmapCollection> GetCollections()
    {
        using var stream = System.IO.File.OpenRead(collectionDbPath);
        using var reader = new BinaryReader(stream);

        reader.ReadInt32(); // client version
        int collectionCount = reader.ReadInt32();

        for (int i = 0; i < collectionCount; i++)
        {
            string name = ReadString(reader);
            int beatmapCount = reader.ReadInt32();
            var hashes = new List<string>(beatmapCount);
            for (int j = 0; j < beatmapCount; j++)
            {
                string hash = ReadString(reader);
                hashes.Add(hash);
            }

            yield return new BeatmapCollection(
                Name: name,
                Hashes: hashes
            );
        }
    }

    public Dictionary<string, List<StableScore>> GetScores()
    {
        if (!System.IO.File.Exists(scoresDbPath))
            return new Dictionary<string, List<StableScore>>();

        using var stream = System.IO.File.OpenRead(scoresDbPath);
        using var reader = new BinaryReader(stream);

        int version = reader.ReadInt32();
        int beatmapCount = reader.ReadInt32();
        var scores = new Dictionary<string, List<StableScore>>();

        for (int i = 0; i < beatmapCount; i++)
        {
            string beatmapHash = ReadString(reader);
            int scoreCount = reader.ReadInt32();
            var beatmapScores = new List<StableScore>();

            for (int j = 0; j < scoreCount; j++)
            {
                byte gameMode = reader.ReadByte();
                int scoreVersion = reader.ReadInt32();
                string beatmapHashRead = ReadString(reader);
                string playerName = ReadString(reader);
                string replayHash = ReadString(reader);
                short count300 = reader.ReadInt16();
                short count100 = reader.ReadInt16();
                short count50 = reader.ReadInt16();
                short countGeki = reader.ReadInt16();
                short countKatu = reader.ReadInt16();
                short countMiss = reader.ReadInt16();
                int replayScore = reader.ReadInt32();
                short maxCombo = reader.ReadInt16();
                bool perfectCombo = reader.ReadBoolean();
                int mods = reader.ReadInt32();
                string emptyString = ReadString(reader);
                long timestamp = reader.ReadInt64();
                int negativeOne = reader.ReadInt32();
                long onlineScoreId = reader.ReadInt64();

                double? additionalModInfo = null;
                if ((mods & (int)BitwiseMods.AT) != 0)
                {
                    additionalModInfo = reader.ReadDouble();
                }

                var score = new StableScore(
                    gameMode,
                    scoreVersion,
                    beatmapHashRead,
                    playerName,
                    replayHash,
                    count300,
                    count100,
                    count50,
                    countGeki,
                    countKatu,
                    countMiss,
                    replayScore,
                    maxCombo,
                    perfectCombo,
                    mods,
                    emptyString,
                    timestamp,
                    negativeOne,
                    onlineScoreId,
                    additionalModInfo
                );

                beatmapScores.Add(score);
            }

            scores[beatmapHash] = beatmapScores;
        }

        return scores;
    }

    internal static string ReadString(BinaryReader r)
    {
        byte indicator = r.ReadByte();
        if (indicator == 0x00) return string.Empty;
        if (indicator != 0x0B) throw new InvalidDataException("Invalid string format");
        int length = ReadULEB128(r);
        byte[] data = r.ReadBytes(length);
        return System.Text.Encoding.UTF8.GetString(data);
    }

    private static int ReadULEB128(BinaryReader r)
    {
        int result = 0;
        int shift = 0;
        while (true)
        {
            byte b = r.ReadByte();
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return result;
    }

    private static Dictionary<int, float> ReadStarRatings(BinaryReader reader, int dbVersion)
    {
        int count = reader.ReadInt32();
        var dict = new Dictionary<int, float>(count);
        for (int i = 0; i < count; i++)
        {
            // int-float pair or int-double pair
            byte indicator1 = reader.ReadByte(); // 0x08
            if (indicator1 != 0x08) throw new Exception("Expected Int-Pair indicator.");
            int mods = reader.ReadInt32();

            byte indicator2;
            if (dbVersion < 20250107)
            {
                indicator2 = reader.ReadByte(); // 0x0d
                if (indicator2 != 0x0d) throw new Exception("Expected Double indicator.");
                dict.Add(mods, (float)reader.ReadDouble());
            }
            else
            {
                indicator2 = reader.ReadByte(); // 0x0c
                if (indicator2 != 0x0c) throw new Exception("Expected Float indicator.");
                dict.Add(mods, reader.ReadSingle());
            }
        }
        return dict;
    }
}