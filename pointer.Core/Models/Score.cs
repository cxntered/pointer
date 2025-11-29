namespace pointer.Core.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

public record Score(
    string BeatmapMD5Hash,
    Ruleset Ruleset,
    User User,
    string MD5Hash,
    DateTimeOffset Date,
    int TotalScore,
    int MaxCombo,
    Statistics Statistics,
    Statistics MaximumStatistics,
    List<Mod> Mods,
    int ID,
    bool IsLegacyScore,
    List<File> Files
);

public record Ruleset(
    string Name,
    int ID,
    string ShortName
);

public record User(
    string Username,
    int ID,
    string Country
);

#pragma warning disable format
public record Statistics(
    [property: JsonPropertyName("none")]            int None,
    [property: JsonPropertyName("miss")]            int Miss,
    [property: JsonPropertyName("meh")]             int Meh,
    [property: JsonPropertyName("ok")]              int Ok,
    [property: JsonPropertyName("good")]            int Good,
    [property: JsonPropertyName("great")]           int Great,
    [property: JsonPropertyName("perfect")]         int Perfect,
    [property: JsonPropertyName("small_tick_miss")] int SmallTickMiss,
    [property: JsonPropertyName("small_tick_hit")]  int SmallTickHit,
    [property: JsonPropertyName("large_tick_miss")] int LargeTickMiss,
    [property: JsonPropertyName("large_tick_hit")]  int LargeTickHit,
    [property: JsonPropertyName("small_bonus")]     int SmallBonus,
    [property: JsonPropertyName("large_bonus")]     int LargeBonus,
    [property: JsonPropertyName("ignore_miss")]     int IgnoreMiss,
    [property: JsonPropertyName("ignore_hit")]      int IgnoreHit,
    [property: JsonPropertyName("combo_break")]     int ComboBreak,
    [property: JsonPropertyName("slider_tail_hit")] int SliderTailHit
);

public enum BitwiseMods
{
    NM  = 0,
    NF  = 1,
    EZ  = 1 << 1,
    TD  = 1 << 2,
    HD  = 1 << 3,
    HR  = 1 << 4,
    SD  = 1 << 5,
    DT  = 1 << 6,
    RX  = 1 << 7,
    HT  = 1 << 8,
    NC  = 1 << 9 | DT, // always set with DT
    FL  = 1 << 10,
    AT  = 1 << 11,
    SO  = 1 << 12,
    AP  = 1 << 13,
    PF  = 1 << 14 | SD, // always set with SD
    _4K = 1 << 15,
    _5K = 1 << 16,
    _6K = 1 << 17,
    _7K = 1 << 18,
    _8K = 1 << 19,
    FI  = 1 << 20,
    RD  = 1 << 21,
    CM  = 1 << 22,
    TP  = 1 << 23,
    _9K = 1 << 24,
    DS  = 1 << 25, // dual stages/coop
    _1K = 1 << 26,
    _3K = 1 << 27,
    _2K = 1 << 28,
    SV2 = 1 << 29,
    MR  = 1 << 30,
}

public record Mod(
    [property: JsonPropertyName("acronym")]  string Acronym,
    [property: JsonPropertyName("settings")] JsonElement? Settings
);
#pragma warning restore format

public record StableScore(
    byte GameMode,
    int Version,
    string BeatmapHash,
    string PlayerName,
    string ReplayHash,
    short Count300,
    short Count100,
    short Count50,
    short CountGeki,
    short CountKatu,
    short CountMiss,
    int ReplayScore,
    short MaxCombo,
    bool PerfectCombo,
    int Mods,
    string HealthGraph,
    long Timestamp,
    int CompressedReplayLength,
    long OnlineScoreId,
    double? AdditionalModInfo
);

