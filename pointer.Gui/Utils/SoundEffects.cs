namespace pointer.Gui.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;

public static class SoundEffects
{
    private static readonly AudioEngine Engine = new MiniAudioEngine();
    private static readonly AudioFormat Format = AudioFormat.DvdHq;
    private static readonly Dictionary<string, byte[]> Assets = [];
    private static AudioPlaybackDevice? _device;
    private static bool _initialized;

    private const float MasterVolume = 0.5f;

    public static void Play(string name)
    {
        var device = Device();
        if (device is null)
        {
            return;
        }

        var player = new SoundPlayer(Engine, Format, new AssetDataProvider(Engine, Format, new MemoryStream(Asset(name))))
        {
            Volume = MasterVolume
        };
        player.PlaybackEnded += (_, _) => device.MasterMixer.RemoveComponent(player);
        device.MasterMixer.AddComponent(player);
        player.Play();
    }

    private static AudioPlaybackDevice? Device()
    {
        if (_initialized)
        {
            return _device;
        }

        _initialized = true;

        try
        {
            _device = Engine.InitializePlaybackDevice(null, Format);
            _device.Start();
        }
        catch
        {
            _device = null; // no audio device, keep the app usable
        }

        return _device;
    }

    private static byte[] Asset(string name)
    {
        if (!Assets.TryGetValue(name, out var data))
        {
            using var stream = AssetLoader.Open(new Uri($"avares://pointer.Gui/Assets/Sounds/{name}.wav"));
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            data = memory.ToArray();
            Assets[name] = data;
        }

        return data;
    }
}
