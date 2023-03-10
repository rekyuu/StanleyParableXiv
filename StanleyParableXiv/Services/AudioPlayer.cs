using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StanleyParableXiv.Services;
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

public class AudioPlayer : IDisposable
{
    public static AudioPlayer Instance { get; } = new();
    
    private readonly IWavePlayer _outputDevice;
    private readonly VolumeSampleProvider _sampleProvider;
    private readonly MixingSampleProvider _mixer;

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
                "announcer_dlc_stanleyparable/announcer_respawn_advice_06.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_07.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_08.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_09.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_10.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_advice_13.mp3",
            }
        },
        { 
            AudioEvent.Afk, new []
            {
                "announcer_dlc_stanleyparable/announcer_idle_01.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_04.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_05.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_06.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_07.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_08.mp3",
                "announcer_dlc_stanleyparable/announcer_idle_09.mp3",
            }
        },
        { 
            AudioEvent.Countdown10, new []
            {
                "announcer_dlc_stanleyparable/announcer_count_battle_10_01.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_02.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_03.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_04.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_10_05.mp3",
            }
        },
        { 
            AudioEvent.CountdownStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_count_battle_30_06.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_07.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_08.mp3",
                "announcer_dlc_stanleyparable/announcer_count_battle_30_09.mp3",
            }
        },
        { 
            AudioEvent.Disconnect, new []
            {
                "announcer_dlc_stanleyparable/announcer_player_disconnect_01.mp3",
                "announcer_dlc_stanleyparable/announcer_player_disconnect_03.mp3",
                "announcer_dlc_stanleyparable/announcer_player_disconnect_04.mp3",
                "announcer_dlc_stanleyparable/announcer_player_quit_01.mp3",
                "announcer_dlc_stanleyparable/announcer_player_quit_05.mp3",
                "announcer_dlc_stanleyparable/announcer_player_quit_06.mp3",
                "announcer_dlc_stanleyparable/announcer_player_quit_07.mp3",
            }
        },
        { 
            AudioEvent.EncounterComplete, new []
            {
                "announcer_dlc_stanleyparable/announcer_victory_01.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_02.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_03.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_04.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_05.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_07.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_09.mp3",
            }
        },
        {
            AudioEvent.EncounterStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_begin.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_01.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_02.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_03.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_05.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_06.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_07.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_08.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_11.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_13.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_14.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_15.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_16.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_17.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_18.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_19.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_01.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_02.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_07.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_08.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_11.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_12.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_13.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_18.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_19.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_21.mp3",
            }
        },
        { 
            AudioEvent.Failure, new []
            {
                "announcer_dlc_stanleyparable/announcer_defeat_01.mp3",
                "announcer_dlc_stanleyparable/announcer_defeat_03.mp3",
                "announcer_dlc_stanleyparable/announcer_defeat_05.mp3",
                "announcer_dlc_stanleyparable/announcer_defeat_06.mp3",
            }
        },
        {
            AudioEvent.FirstBlood, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_04.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_1stblood_05.mp3",
            }
        },
        {
            AudioEvent.KillStreak3, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_spree_04.mp3",
            }
        },
        {
            AudioEvent.KillStreak4, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_dominate_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_dominate_02.mp3",
            }
        },
        {
            AudioEvent.KillStreak5, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_mega_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_mega_02.mp3",
            }
        },
        {
            AudioEvent.KillStreak6, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_unstop_03.mp3",
            }
        },
        {
            AudioEvent.KillStreak7, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wicked_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wicked_02.mp3",
            }
        },
        {
            AudioEvent.KillStreak8, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_monster_03.mp3",
            }
        },
        {
            AudioEvent.KillStreak9, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_godlike_03.mp3",
            }
        },
        {
            AudioEvent.KillStreak10, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_04.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_05.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_06.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_07.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_08.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_09.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_10.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_11.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_12.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_13.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_14.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_15.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_16.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_17.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_18.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_19.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_20.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_21.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_22.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_23.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_24.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_25.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_26.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_27.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_28.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_holy_29.mp3",
            }
        },
        { 
            AudioEvent.Login, new []
            {
                "announcer_dlc_stanleyparable/announcer_welcome_02.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_03.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_04.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_05.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_09.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_10.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_11.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_12.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_14.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_15.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_17.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_19.mp3",
                "announcer_dlc_stanleyparable/announcer_welcome_21.mp3",
            }
        },
        { 
            AudioEvent.Multikill2, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_04.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_05.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_06.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_07.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_double_08.mp3",
            }
        },
        { 
            AudioEvent.Multikill3, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_triple_03.mp3",
            }
        },
        { 
            AudioEvent.Multikill4, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_ultra_03.mp3",
            }
        },
        { 
            AudioEvent.Multikill5, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_01.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_rampage_04.mp3",
            }
        },
        { 
            AudioEvent.MarketBoardPurchase, new []
            {
                "announcer_dlc_stanleyparable/announcer_purchase_01.mp3",
                "announcer_dlc_stanleyparable/announcer_purchase_02.mp3",
                "announcer_dlc_stanleyparable/announcer_purchase_03.mp3",
                "announcer_dlc_stanleyparable/announcer_purchase_04.mp3",
                "announcer_dlc_stanleyparable/announcer_purchase_05.mp3",
            }
        },
        { 
            AudioEvent.PvpPrepare, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_prepare_01.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_02.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_03.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_04.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_05.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_06.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_07.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_08.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_11.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_12.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_13.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_14.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_18.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_19.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_prepare_21.mp3",
            }
        },
        {
            AudioEvent.PvpStart, new []
            {
                "announcer_dlc_stanleyparable/announcer_battle_begin.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_01.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_02.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_03.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_04.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_05.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_06.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_07.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_08.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_09.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_10.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_11.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_13.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_14.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_15.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_16.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_17.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_18.mp3",
                "announcer_dlc_stanleyparable/announcer_battle_begin_19.mp3",
            }
        },
        { 
            AudioEvent.PvpWin, new []
            {
                "announcer_dlc_stanleyparable/announcer_victory_01.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_02.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_03.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_04.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_05.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_07.mp3",
                "announcer_dlc_stanleyparable/announcer_victory_09.mp3",
            }
        },
        { 
            AudioEvent.Respawn, new []
            {
                "announcer_dlc_stanleyparable/announcer_respawn_01.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_02.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_04.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_05.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_06.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_07.mp3",
                "announcer_dlc_stanleyparable/announcer_respawn_09.mp3",
            }
        },
        { 
            AudioEvent.Reconnect, new []
            {
                "announcer_dlc_stanleyparable/announcer_player_reconnect_01.mp3",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_02.mp3",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_04.mp3",
                "announcer_dlc_stanleyparable/announcer_player_reconnect_05.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_01.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_02.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_03.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_04.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_05.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_07.mp3",
                "announcer_dlc_stanleyparable/announcer_unpause_08.mp3",
            }
        },
        { 
            AudioEvent.ShrimpFact, new []
            {
                "announcer_dlc_stanleyparable/announcer_shrimp_01.mp3",
                "announcer_dlc_stanleyparable/announcer_shrimp_02.mp3",
                "announcer_dlc_stanleyparable/announcer_shrimp_03.mp3",
                "announcer_dlc_stanleyparable/announcer_shrimp_04.mp3",
                "announcer_dlc_stanleyparable/announcer_shrimp_05.mp3",
            }
        },
        { 
            AudioEvent.Wipe, new []
            {
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_04.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_05.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_06.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_07.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_kill_wipeout_you_08.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_02.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_03.mp3",
                "announcer_dlc_stanleyparable_killing_spree/announcer_ownage_them_04.mp3",
            }
        },
    };

    /// <summary>
    /// Service used for playing audio files from the Resources folder.
    /// Audio mixer implementation referenced from https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public AudioPlayer()
    {
        _outputDevice = new WaveOutEvent();
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
        {
            ReadFully = true
        };
        _sampleProvider = new VolumeSampleProvider(_mixer);

        _mixer.MixerInputEnded += OnMixerInputEnded;
        
        _outputDevice.Init(_sampleProvider);
        _outputDevice.Play();
        
        UpdateVolume();
    }

    public void Dispose()
    {
        _outputDevice.Dispose();
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

                if (baseVolume == 0) targetVolume = 0;
                else targetVolume = Math.Clamp((baseVolume + baseVolumeBoost) * (masterVolume / 100f), 0, 100) / 100f;
            }
            // Updates the volume from the configured volume amount.
            else
            {
                targetVolume = Math.Clamp(Configuration.Instance.Volume, 0, 100) / 100f;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Exception was thrown while setting volume");
        }
        finally
        {
            PluginLog.Debug("Setting volume to {TargetVolume}", targetVolume);
            _sampleProvider.Volume = targetVolume;
        }

        _originalVolume = _sampleProvider.Volume;
        _killingSpreeVolume = _sampleProvider.Volume * 0.70f;
    }
    
    /// <summary>
    /// Plays a random sound from the supplied event enum.
    /// </summary>
    /// <param name="event">The event category.</param>
    public void PlayRandomSoundFromCategory(AudioEvent @event)
    {
        lock (_lockObj)
        {
            if (_isPlaying) return;
        
            string lastPlayed = "";
            if (!_lastPlayedByEvent.ContainsKey(@event)) _lastPlayedByEvent[@event] = lastPlayed;
            else lastPlayed = _lastPlayedByEvent[@event];

            Random random = new();
            string[] choices = _audioEventMap[@event].Where(x => x != lastPlayed).ToArray();
            int index = random.Next(0, choices.Length);
            string result = choices[index];

            // Fix killing spree lines being louder than others
            if (result.StartsWith("announcer_dlc_stanleyparable_killing_spree"))
            {
                PluginLog.Debug("Lowering volume for Killing Spree line");
                _sampleProvider.Volume = _killingSpreeVolume;
            }

            PluginLog.Debug("Playing {Result} for event {Event}", result, @event);
            PlaySound(result);

            if (result == "announcer_dlc_stanleyparable/announcer_respawn_09.mp3") _adviceFollowUp = true;
            if (result == "announcer_dlc_stanleyparable/announcer_idle_09.mp3") _shrimpFactFollowUp = true;
            
            _lastPlayedClip = result;
            _lastPlayedByEvent[@event] = result;
        }
    }

    /// <summary>
    /// Plays a sound from the supplied path in the Resources directory.
    /// </summary>
    /// <param name="resourcePath">The path of the file to paly.</param>
    public void PlaySound(string resourcePath)
    {
        if (_isPlaying) return;

        string audioPath = $"{DalamudService.PluginInterface.GetPluginConfigDirectory()}/assets/{resourcePath}";
        PluginLog.Debug("Attempting to play {ResourcePath}", resourcePath);

        if (!File.Exists(audioPath))
        {
            PluginLog.Error("Audio file does not exist: {AudioPath}", audioPath);
            return;
        }

        using AudioFileReader audioFile = new(audioPath);
        _mixer.AddMixerInput(ConvertToCorrectChannelCount(audioFile));
        _isPlaying = true;
    }

    private ISampleProvider ConvertToCorrectChannelCount(ISampleProvider input)
    {
        int inputChannels = input.WaveFormat.Channels;
        int mixerChannels = _mixer.WaveFormat.Channels;
        
        if (inputChannels == mixerChannels) return input;
        if (inputChannels == 1 && mixerChannels == 2)
        {
            return new MonoToStereoSampleProvider(input);
        }
        
        throw new NotImplementedException($"Conversion from {inputChannels} to {mixerChannels} channels is not yet implemented");
    }

    private void OnMixerInputEnded(object? sender, SampleProviderEventArgs e)
    {
        _isPlaying = false;
        
        // Restore fix killing spree lines being louder than others
        if (_lastPlayedClip.StartsWith("announcer_dlc_stanleyparable_killing_spree"))
        {
            PluginLog.Debug("Restoring volume for Killing Spree line");
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