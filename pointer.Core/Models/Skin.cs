using Realms;

namespace pointer.Core.Models;

public record Skin(
    string Name,
    string? IniName,
    string InstantiationInfo,
    List<File> Files
)
{
    public static Skin FromDynamic(IRealmObject skin)
    {
        return new Skin(
            Name: skin.DynamicApi.Get<string>("Name"),
            IniName: null,
            InstantiationInfo: skin.DynamicApi.Get<string>("InstantiationInfo"),
            Files: skin.DynamicApi.GetList<IRealmObjectBase>("Files")
                .Select(File.FromDynamic)
                .ToList()
        );
    }
}
