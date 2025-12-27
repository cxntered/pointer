using Realms;

namespace pointer.Core.Models;

public record File(
    string Filename,
    string Hash
)
{
    public static File FromDynamic(IRealmObjectBase file)
    {
        return new File(
            Filename: file.DynamicApi.Get<string>("Filename"),
            Hash: file.DynamicApi.Get<IRealmObjectBase>("File").DynamicApi.Get<string>("Hash")
        );
    }
}
