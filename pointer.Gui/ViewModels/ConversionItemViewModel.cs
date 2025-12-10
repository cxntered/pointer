using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using pointer.Core;
using pointer.Core.Models;

namespace pointer.Gui.ViewModels;

public enum ConversionItemType
{
    Beatmaps,
    Scores,
    Skins,
    Collections
}

public partial class ConversionItemViewModel(
    string name,
    Func<ConversionManager, IEnumerable<object>> getItems,
    Action<ConversionManager, IEnumerable<object>> convertItems) : ObservableObject
{
    [ObservableProperty]
    public partial string DisplayText { get; set; } = name;

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = true;

    public string Name { get; } = name;

    private int _count;
    public int Count
    {
        get => _count;
        set
        {
            _count = value;
            string itemWord = value == 1 ? "item" : "items";
            DisplayText = $"{Name} ({value} {itemWord})";
        }
    }

    public static ConversionItemViewModel Create(ConversionItemType type) => type switch
    {
        ConversionItemType.Beatmaps => new("Beatmaps",
            m => m.GetBeatmapSetsToConvert().Cast<object>(),
            (m, items) => m.ConvertBeatmaps(items.Cast<BeatmapSetInfo>())),
        ConversionItemType.Scores => new("Scores",
            m => m.GetScoresToConvert().Cast<object>(),
            (m, items) => m.ConvertScores(items.Cast<Score>())),
        ConversionItemType.Skins => new("Skins",
            m => m.GetSkinsToConvert().Cast<object>(),
            (m, items) => m.ConvertSkins(items.Cast<Skin>())),
        ConversionItemType.Collections => new("Collections",
            m => m.GetCollectionsToConvert().Cast<object>(),
            (m, items) => m.ConvertCollections(items.Cast<BeatmapCollection>())),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private IEnumerable<object>? _itemsToConvert;

    public void LoadItems(ConversionManager manager)
    {
        var items = getItems(manager).ToList();
        _itemsToConvert = items;
        Count = items.Count;
    }

    public void ConvertItems(ConversionManager manager)
    {
        if (_itemsToConvert != null)
            convertItems(manager, _itemsToConvert);
    }

    public void SetCalculating()
    {
        DisplayText = $"{Name} (calculating...)";
    }

    public void Clear()
    {
        _itemsToConvert = null;
        _count = 0;
        DisplayText = Name;
    }
}
