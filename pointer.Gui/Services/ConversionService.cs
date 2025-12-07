using System.Collections.Generic;
using System.Threading.Tasks;
using pointer.Core;
using pointer.Core.Readers;
using pointer.Core.Utils;

namespace pointer.Gui.Services;

public class ConversionService
{
    public LazerDatabaseReader? LazerReader { get; private set; }
    public StableDatabaseReader? StableReader { get; private set; }

    public async Task<Dictionary<string, int>> LoadItemCountsAsync(string lazerPath, string stablePath)
    {
        return await Task.Run(() =>
        {
            LazerReader = new LazerDatabaseReader(lazerPath);
            StableReader = new StableDatabaseReader(stablePath);

            var stableSongsPath = PathResolver.GetStableSongsPath(stablePath);
            var manager = new ConversionManager(LazerReader, StableReader, lazerPath, stablePath, stableSongsPath);

            return new Dictionary<string, int>
            {
                ["Beatmaps"] = manager.GetBeatmapsToConvertCount(),
                ["Scores"] = manager.GetScoresToConvertCount(),
                ["Skins"] = manager.GetSkinsToConvertCount(),
                ["Collections"] = manager.GetCollectionsToConvertCount()
            };
        });
    }

    public async Task ConvertItemsAsync(
        string lazerPath,
        string stablePath,
        Dictionary<string, bool> itemsToConvert)
    {
        await Task.Run(() =>
        {
            var stableSongsPath = PathResolver.GetStableSongsPath(stablePath);
            var manager = new ConversionManager(LazerReader!, StableReader!, lazerPath, stablePath, stableSongsPath);

            if (itemsToConvert.GetValueOrDefault("Beatmaps"))
                manager.ConvertBeatmaps();

            if (itemsToConvert.GetValueOrDefault("Scores"))
                manager.ConvertScores();

            if (itemsToConvert.GetValueOrDefault("Skins"))
                manager.ConvertSkins();

            if (itemsToConvert.GetValueOrDefault("Collections"))
                manager.ConvertCollections();
        });
    }
}
