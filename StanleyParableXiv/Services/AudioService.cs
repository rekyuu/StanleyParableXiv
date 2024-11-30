using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StanleyParableXiv.Utility;

namespace StanleyParableXiv.Services;

public enum AudioEvent
{
    Advice,
    Afk,
    Countdown10,
    CountdownStart,
    Disconnect,
    EncounterComplete,
    EncounterStart,
    Failure,
    FirstBlood,
    KillStreak3,
    KillStreak4,
    KillStreak5,
    KillStreak6,
    KillStreak7,
    KillStreak8,
    KillStreak9,
    KillStreak10,
    Login,
    MarketBoardPurchase,
    Multikill2,
    Multikill3,
    Multikill4,
    Multikill5,
    PvpPrepare,
    PvpStart,
    PvpWin,
    Respawn,
    Reconnect,
    ShrimpFact,
    Wipe
}

public enum OutputType
{
    WaveOut,
    DirectSound,
    Asio,
    Wasapi
}

public class AudioService : IDisposable
{
    public static AudioService Instance { get; } = new();

    public Dictionary<string, Guid> DirectOutAudioDevices = [];
    public List<string> AsioAudioDevices = [];
    public Dictionary<string, string> WasapiAudioDevices = [];

    public string? OutputDeviceFailureException = null;

    private readonly VolumeSampleProvider? _sampleProvider;
    private readonly MixingSampleProvider? _mixer;
    private IWavePlayer? _outputDevice;

    private float _originalVolume;
    private float _killingSpreeVolume;
    
    private bool _isPlaying = false;
    private bool _adviceFollowUp = false;
    private bool _shrimpFactFollowUp = false;
    private string _lastPlayedClip = "";
    private readonly Dictionary<AudioEvent, string> _lastPlayedByEvent = new();

    private readonly object _lockObj = new();
    
    private readonly Dictionary<AudioEvent, string[]> _audioEventMap = new()
    {
        { 
            AudioEvent.Advice, new []
            {
                "announcer_dlc_stanleyparable/announcer_respawn_advice_06",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_07",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_08",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_09",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_10",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_13",
            }
        },
        { 
            AudioEvent.Afk, new []
            {
                "announcer_dlc_stanleyparable/announcer_idle_01",
                "announcer_dlc_stanleyparable/announcer_idle_04",
                "announcer_dlc_stanleyparable/announcer_idle_05",
                "announcer_dlc_stanleyparable/announcer_idle_06",
                "announcer_dlc_stanleyparable/announcer_idle_07",
                "announcer_dlc_stanleyparable/announcer_idle_08",
                "announcer_dlc_stanleyparable/announcer_idle_09",
            }
        },
        { 
            AudioEvent.Countdown10, new []
            {
                "announcer_dlc_stanleyparable/announcer_count_battle_10_01",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_02",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_03",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_04",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_05",
            }
        },
        { 
            AudioEvent.CountdownStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_count_battle_30_06",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_07",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_08",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_09",
            }
        },
        { 
            AudioEvent.Disconnect, new []
            {
                "announcer_dlc_stanleyparable/announcer_player_disconnect_01",
                "announcer_dlc_stanleyparable/announcer_player_disconnect_03",
                "announcer_dlc_stanleyparable/announcer_player_disconnect_04",
                "announcer_dlc_stanleyparable/announcer_player_quit_01",
                "announcer_dlc_stanleyparable/announcer_player_quit_05",
                "announcer_dlc_stanleyparable/announcer_player_quit_06",
                "announcer_dlc_stanleyparable/announcer_player_quit_07",
            }
        },
        { 
            AudioEvent.EncounterComplete, new []
            {
                "announcer_dlc_stanleyparable/announcer_victory_01",
                "announcer_dlc_stanleyparable/announcer_victory_02",
                "announcer_dlc_stanleyparable/announcer_victory_03",
                "announcer_dlc_stanleyparable/announcer_victory_04",
                "announcer_dlc_stanleyparable/announcer_victory_05",
                "announcer_dlc_stanleyparable/announcer_victory_07",
                "announcer_dlc_stanleyparable/announcer_victory_09",
            }
        },
        {
            AudioEvent.EncounterStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_begin",
                "announcer_dlc_stanleyparable/announcer_battle_begin_01",
                "announcer_dlc_stanleyparable/announcer_battle_begin_02",
                "announcer_dlc_stanleyparable/announcer_battle_begin_03",
                "announcer_dlc_stanleyparable/announcer_battle_begin_05",
                "announcer_dlc_stanleyparable/announcer_battle_begin_06",
                "announcer_dlc_stanleyparable/announcer_battle_begin_07",
                "announcer_dlc_stanleyparable/announcer_battle_begin_08",
                "announcer_dlc_stanleyparable/announcer_battle_begin_11",
                "announcer_dlc_stanleyparable/announcer_battle_begin_13",
                "announcer_dlc_stanleyparable/announcer_battle_begin_14",
                "announcer_dlc_stanleyparable/announcer_battle_begin_15",
                "announcer_dlc_stanleyparable/announcer_battle_begin_16",
                "announcer_dlc_stanleyparable/announcer_battle_begin_17",
                "announcer_dlc_stanleyparable/announcer_battle_begin_18",
                "announcer_dlc_stanleyparable/announcer_battle_begin_19",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_01",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_02",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_07",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_08",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_11",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_12",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_13",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_18",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_19",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_21",
            }
        },
        { 
            AudioEvent.Failure, new []
            {
                "announcer_dlc_stanleyparable/announcer_defeat_01",
                "announcer_dlc_stanleyparable/announcer_defeat_03",
                "announcer_dlc_stanleyparable/announcer_defeat_05",
                "announcer_dlc_stanleyparable/announcer_defeat_06",
            }
        },
        {
            AudioEvent.FirstBlood, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_04",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_05",
            }
        },
        {
            AudioEvent.KillStreak3, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_04",
            }
        },
        {
            AudioEvent.KillStreak4, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_dominate_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_dominate_02",
            }
        },
        {
            AudioEvent.KillStreak5, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_mega_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_mega_02",
            }
        },
        {
            AudioEvent.KillStreak6, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_03",
            }
        },
        {
            AudioEvent.KillStreak7, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wicked_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wicked_02",
            }
        },
        {
            AudioEvent.KillStreak8, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_03",
            }
        },
        {
            AudioEvent.KillStreak9, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_03",
            }
        },
        {
            AudioEvent.KillStreak10, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_04",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_05",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_06",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_07",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_08",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_09",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_10",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_11",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_12",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_13",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_14",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_15",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_16",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_17",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_18",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_19",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_20",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_21",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_22",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_23",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_24",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_25",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_26",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_27",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_28",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_29",
            }
        },
        { 
            AudioEvent.Login, new []
            {
                "announcer_dlc_stanleyparable/announcer_welcome_02",
                "announcer_dlc_stanleyparable/announcer_welcome_03",
                "announcer_dlc_stanleyparable/announcer_welcome_04",
                "announcer_dlc_stanleyparable/announcer_welcome_05",
                "announcer_dlc_stanleyparable/announcer_welcome_09",
                "announcer_dlc_stanleyparable/announcer_welcome_10",
                "announcer_dlc_stanleyparable/announcer_welcome_11",
                "announcer_dlc_stanleyparable/announcer_welcome_12",
                "announcer_dlc_stanleyparable/announcer_welcome_14",
                "announcer_dlc_stanleyparable/announcer_welcome_15",
                "announcer_dlc_stanleyparable/announcer_welcome_17",
                "announcer_dlc_stanleyparable/announcer_welcome_19",
                "announcer_dlc_stanleyparable/announcer_welcome_21",
            }
        },
        { 
            AudioEvent.Multikill2, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_04",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_05",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_06",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_07",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_08",
            }
        },
        { 
            AudioEvent.Multikill3, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_03",
            }
        },
        { 
            AudioEvent.Multikill4, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_03",
            }
        },
        { 
            AudioEvent.Multikill5, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_01",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_04",
            }
        },
        { 
            AudioEvent.MarketBoardPurchase, new []
            {
                "announcer_dlc_stanleyparable/announcer_purchase_01",
                "announcer_dlc_stanleyparable/announcer_purchase_02",
                "announcer_dlc_stanleyparable/announcer_purchase_03",
                "announcer_dlc_stanleyparable/announcer_purchase_04",
                "announcer_dlc_stanleyparable/announcer_purchase_05",
            }
        },
        { 
            AudioEvent.PvpPrepare, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_prepare_01",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_02",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_03",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_04",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_05",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_06",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_07",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_08",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_11",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_12",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_13",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_14",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_18",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_19",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_21",
            }
        },
        {
            AudioEvent.PvpStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_begin",
                "announcer_dlc_stanleyparable/announcer_battle_begin_01",
                "announcer_dlc_stanleyparable/announcer_battle_begin_02",
                "announcer_dlc_stanleyparable/announcer_battle_begin_03",
                "announcer_dlc_stanleyparable/announcer_battle_begin_04",
                "announcer_dlc_stanleyparable/announcer_battle_begin_05",
                "announcer_dlc_stanleyparable/announcer_battle_begin_06",
                "announcer_dlc_stanleyparable/announcer_battle_begin_07",
                "announcer_dlc_stanleyparable/announcer_battle_begin_08",
                "announcer_dlc_stanleyparable/announcer_battle_begin_09",
                "announcer_dlc_stanleyparable/announcer_battle_begin_10",
                "announcer_dlc_stanleyparable/announcer_battle_begin_11",
                "announcer_dlc_stanleyparable/announcer_battle_begin_13",
                "announcer_dlc_stanleyparable/announcer_battle_begin_14",
                "announcer_dlc_stanleyparable/announcer_battle_begin_15",
                "announcer_dlc_stanleyparable/announcer_battle_begin_16",
                "announcer_dlc_stanleyparable/announcer_battle_begin_17",
                "announcer_dlc_stanleyparable/announcer_battle_begin_18",
                "announcer_dlc_stanleyparable/announcer_battle_begin_19",
            }
        },
        { 
            AudioEvent.PvpWin, new []
            {
                "announcer_dlc_stanleyparable/announcer_victory_01",
                "announcer_dlc_stanleyparable/announcer_victory_02",
                "announcer_dlc_stanleyparable/announcer_victory_03",
                "announcer_dlc_stanleyparable/announcer_victory_04",
                "announcer_dlc_stanleyparable/announcer_victory_05",
                "announcer_dlc_stanleyparable/announcer_victory_07",
                "announcer_dlc_stanleyparable/announcer_victory_09",
            }
        },
        { 
            AudioEvent.Respawn, new []
            {
                "announcer_dlc_stanleyparable/announcer_respawn_01",
                "announcer_dlc_stanleyparable/announcer_respawn_02",
                "announcer_dlc_stanleyparable/announcer_respawn_04",
                "announcer_dlc_stanleyparable/announcer_respawn_05",
                "announcer_dlc_stanleyparable/announcer_respawn_06",
                "announcer_dlc_stanleyparable/announcer_respawn_07",
                "announcer_dlc_stanleyparable/announcer_respawn_09",
            }
        },
        { 
            AudioEvent.Reconnect, new []
            {
                "announcer_dlc_stanleyparable/announcer_player_reconnect_01",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_02",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_04",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_05",
                "announcer_dlc_stanleyparable/announcer_unpause_01",
                "announcer_dlc_stanleyparable/announcer_unpause_02",
                "announcer_dlc_stanleyparable/announcer_unpause_03",
                "announcer_dlc_stanleyparable/announcer_unpause_04",
                "announcer_dlc_stanleyparable/announcer_unpause_05",
                "announcer_dlc_stanleyparable/announcer_unpause_07",
                "announcer_dlc_stanleyparable/announcer_unpause_08",
            }
        },
        { 
            AudioEvent.ShrimpFact, new []
            {
                "announcer_dlc_stanleyparable/announcer_shrimp_01",
                "announcer_dlc_stanleyparable/announcer_shrimp_02",
                "announcer_dlc_stanleyparable/announcer_shrimp_03",
                "announcer_dlc_stanleyparable/announcer_shrimp_04",
                "announcer_dlc_stanleyparable/announcer_shrimp_05",
            }
        },
        { 
            AudioEvent.Wipe, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_04",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_05",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_06",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_07",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_08",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_02",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_03",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_04",
            }
        },
    };

    /// <summary>
    /// Service used for playing audio files from the Resources folder.
    /// Audio mixer implementation referenced from https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public AudioService()
    {
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };
        _sampleProvider = new VolumeSampleProvider(_mixer);
        _mixer.MixerInputEnded += OnMixerInputEnded;

        InitializeOutputDevice();
        UpdateAudioDevices();
        UpdateVolume();
    }

    public void Dispose()
    {
        _outputDevice?.Dispose();
        if (_mixer != null) _mixer.MixerInputEnded -= OnMixerInputEnded;

        GC.SuppressFinalize(this);
    }

    public void InitializeOutputDevice()
    {
        _outputDevice?.Dispose();
        OutputDeviceFailureException = null;

        DalamudService.Log.Debug("Initializing audio device using {Type}", Configuration.Instance.OutputType);

        try
        {
            // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md
            _outputDevice = Configuration.Instance.OutputType switch
            {
                // WaveOut device selection will likely never be supported since it, for some reason,
                // requires WinForms libraries. It will select the default audio device instead.
                OutputType.WaveOut => new WaveOutEvent(),
                OutputType.DirectSound when Configuration.Instance.DirectOutDevice == Guid.Empty => new DirectSoundOut(),
                OutputType.DirectSound => new DirectSoundOut(Configuration.Instance.DirectOutDevice),
                OutputType.Asio when string.IsNullOrEmpty(Configuration.Instance.AsioDevice) => new AsioOut(),
                OutputType.Asio when Configuration.Instance.AsioDevice == "Default" => new AsioOut(),
                OutputType.Asio => new AsioOut(Configuration.Instance.AsioDevice),
                OutputType.Wasapi when string.IsNullOrEmpty(Configuration.Instance.WasapiDevice) => new WasapiOut(),
                OutputType.Wasapi => new WasapiOut(GetWasapiAudioDevice(Configuration.Instance.WasapiDevice), AudioClientShareMode.Shared, true, 200),
                _ => throw new ArgumentOutOfRangeException()
            };

            _outputDevice.Init(_sampleProvider);
            _outputDevice.Play();
        }
        catch (Exception ex)
        {
            OutputDeviceFailureException = ex.Message;
        }
    }

    public void UpdateAudioDevices()
    {
        DirectOutAudioDevices = new Dictionary<string, Guid> {{"Default", Guid.Empty}};
        foreach (DirectSoundDeviceInfo? device in DirectSoundOut.Devices)
        {
            DirectOutAudioDevices.TryAdd(device.Description, device.Guid);
        }

        AsioAudioDevices = ["Default"];
        foreach (string device in AsioOut.GetDriverNames())
        {
            AsioAudioDevices.Add(device);
        }

        WasapiAudioDevices = new Dictionary<string, string> {{"Default", ""}};
        MMDeviceEnumerator enumerator = new();
        foreach (MMDevice? device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (device == null) continue;
            WasapiAudioDevices.TryAdd($"{device.FriendlyName}", device.ID);
        }
    }

    private MMDevice? GetWasapiAudioDevice(string id)
    {
        MMDeviceEnumerator enumerator = new();
        foreach (MMDevice? device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            if (device == null) continue;
            if (device.ID == id) return device;
        }

        return null;
    }

    public static float GetBoundVolume(uint baseVolume, uint masterVolume, uint baseVolumeBoost)
    {
        if (baseVolume == 0) return 0;
        return Math.Clamp((baseVolume + baseVolumeBoost) * (masterVolume / 100f), 0, 100) / 100f;
    }

    /// <summary>
    /// Updates the volume of the output mixer.
    /// </summary>
    public void UpdateVolume()
    {
        float targetVolume = 0.5f;

        try
        {
            // Updates the volume using the FFXIV configured volume.
            // Takes the configured volume source and multiplies it by the master volume amount.
            // Additionally adds volume boost before applying the master volume multiplier.
            if (Configuration.Instance.BindToXivVolumeSource)
            {
                uint baseVolume = XivUtility.GetVolume(Configuration.Instance.XivVolumeSource);
                uint masterVolume = XivUtility.GetVolume(XivVolumeSource.Master);
                uint baseVolumeBoost = Configuration.Instance.XivVolumeSourceBoost;

                targetVolume = GetBoundVolume(baseVolume, masterVolume, baseVolumeBoost);
            }
            // Updates the volume from the configured volume amount.
            else
            {
                targetVolume = Math.Clamp(Configuration.Instance.Volume, 0, 100) / 100f;
            }
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "Exception was thrown while setting volume");
        }

        if (_sampleProvider == null) return;

        DalamudService.Log.Debug("Setting volume to {TargetVolume}", targetVolume);
        _sampleProvider.Volume = targetVolume;

        _originalVolume = _sampleProvider?.Volume ?? 0.5f;
        _killingSpreeVolume = _sampleProvider?.Volume * 0.70f ?? 0.5f;
    }
    
    /// <summary>
    /// Plays a random sound from the supplied event enum.
    /// </summary>
    /// <param name="event">The event category.</param>
    public void PlayRandomSoundFromCategory(AudioEvent @event)
    {
        if (_sampleProvider == null) return;

        lock (_lockObj)
        {
            if (_isPlaying) return;
        
            string lastPlayed = "";
            if (!_lastPlayedByEvent.TryAdd(@event, lastPlayed)) lastPlayed = _lastPlayedByEvent[@event];

            Random random = new();
            string[] choices = _audioEventMap[@event].Where(x => x != lastPlayed).ToArray();
            int index = random.Next(0, choices.Length);
            string result = choices[index];

            // Fix killing spree lines being louder than others
            if (result.StartsWith("announcer_dlc_stanleyparable_killing_spree"))
            {
                DalamudService.Log.Debug("Lowering volume for Killing Spree line");
                _sampleProvider.Volume = _killingSpreeVolume;
            }

            DalamudService.Log.Debug("Playing {Result} for event {Event}", result, @event);
            PlaySound(result);

            if (result == "announcer_dlc_stanleyparable/announcer_respawn_09") _adviceFollowUp = true;
            if (result == "announcer_dlc_stanleyparable/announcer_idle_09") _shrimpFactFollowUp = true;
            
            _lastPlayedClip = result;
            _lastPlayedByEvent[@event] = result;
        }
    }

    /// <summary>
    /// Plays a sound from the supplied path in the Resources directory.
    /// </summary>
    /// <param name="resourcePath">The path of the file to play.</param>
    public void PlaySound(string resourcePath)
    {
        if (_sampleProvider == null || _mixer == null) return;
        if (_isPlaying) return;

        string audioPath = GetFilepathForResource(resourcePath);
        DalamudService.Log.Debug("Attempting to play {AudioPath}", audioPath);

        if (!File.Exists(audioPath))
        {
            DalamudService.Log.Error("Audio file does not exist: {AudioPath}", audioPath);
            return;
        }

        try
        {
            using AudioFileReader audioFile = new(audioPath);
            _mixer.AddMixerInput(ConvertToCorrectChannelCount(audioFile));
            _isPlaying = true;
        }
        catch (Exception e)
        {
            DalamudService.Log.Error(e, "An exception was thrown while attempting to play audio file");
        }
    }

    private static string GetFilepathForResource(string resourcePath)
    {
        string assetsDir = AssetsManager.GetAssetsDirectory();
        string baseAudioPath = $"{assetsDir}/{resourcePath}";
        
        return Configuration.Instance.AssetsFileType switch
        {
            AssetsFileType.Mp3 => $"{baseAudioPath}.mp3",
            AssetsFileType.Ogg => $"{baseAudioPath}.ogg",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private ISampleProvider ConvertToCorrectChannelCount(ISampleProvider input)
    {
        int inputChannels = input.WaveFormat.Channels;
        int? mixerChannels = _mixer?.WaveFormat.Channels;
        
        if (inputChannels == mixerChannels) return input;
        if (inputChannels == 1 && mixerChannels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }
        
        throw new NotImplementedException($"Conversion from {inputChannels} to {mixerChannels} channels is not yet implemented");
    }

    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        if (_sampleProvider == null) return;
        _isPlaying = false;
        
        // Restore fix killing spree lines being louder than others
        if (_lastPlayedClip.StartsWith("announcer_dlc_stanleyparable_killing_spree"))
        {
            DalamudService.Log.Debug("Restoring volume for Killing Spree line");
            _sampleProvider.Volume = _originalVolume;
        }
        
        // Chains sounds together when needed.
        if (_shrimpFactFollowUp)
        {
            PlayRandomSoundFromCategory(AudioEvent.ShrimpFact);
            _shrimpFactFollowUp = false;
        }

        if (_adviceFollowUp)
        {
            PlayRandomSoundFromCategory(AudioEvent.Advice);
            _adviceFollowUp = false;
        }
    }
}