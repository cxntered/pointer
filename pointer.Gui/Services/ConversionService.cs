using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pointer.Core;
using pointer.Core.Readers;
using pointer.Core.Utils;
using pointer.Gui.ViewModels;

namespace pointer.Gui.Services;

public class ConversionService
{
    private ConversionManager? _manager;

    public async Task LoadItemsToConvertAsync(
        string lazerPath,
        string stablePath,
        IEnumerable<ConversionItemViewModel> items)
    {
        await Task.Run(() =>
        {
            var lazerReader = new LazerDatabaseReader(lazerPath);
            var stableReader = new StableDatabaseReader(stablePath);

            var stableSongsPath = PathResolver.GetStableSongsPath(stablePath);
            _manager = new ConversionManager(lazerReader, stableReader, lazerPath, stablePath, stableSongsPath);

            Parallel.ForEach(items, item => item.LoadItems(_manager));
        });
    }

    public async Task ConvertItemsAsync(IEnumerable<ConversionItemViewModel> items, IProgress<double> progress)
    {
        var itemsToConvert = items.Where(i => i.IsChecked && i.Count > 0).ToList();
        int total = itemsToConvert.Count;
        int completed = 0;

        await Task.Run(() =>
        {
            Parallel.ForEach(itemsToConvert, item =>
            {
                item.ConvertItems(_manager!);
                Interlocked.Increment(ref completed);
                double percentage = (double)completed / total * 100;
                progress?.Report(percentage);
            });
        });
    }
}
