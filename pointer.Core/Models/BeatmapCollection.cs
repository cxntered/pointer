using Realms;

namespace pointer.Core.Models;

public record BeatmapCollection(
    string Name,
    List<string> Hashes
)
{
    public static BeatmapCollection FromDynamic(IRealmObject collection)
    {
        return new BeatmapCollection(
            Name: collection.DynamicApi.Get<string>("Name"),
            Hashes: collection.DynamicApi.GetList<string>("BeatmapMD5Hashes").ToList()
        );
    }
}
