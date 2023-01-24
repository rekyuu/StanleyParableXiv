using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Plugin;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace StanleyParableXiv;

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
    Login,
    MarketboardPurchase,
    PvpPrepare,
    PvpStart,
    PvpWin,
    Respawn,
    Reconnect,
    ShrimpFact,
}

public class AudioPlayer : IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly Configuration _configuration;
    
    private readonly IWavePlayer _outputDevice;
    private readonly VolumeSampleProvider _sampleProvider;
    private readonly MixingSampleProvider _mixer;

    private bool _isPlaying = false;
    private bool _adviceFollowUp = false;
    private bool _shrimpFactFollowUp = false;
    private readonly Dictionary<AudioEvent, string> _lastPlayed = new();

    private object _lockObj = new();
    
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
            AudioEvent.MarketboardPurchase, new []
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
                "announcer_dlc_stanleyparable/announcer_respawn_03.mp3",
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
                "announcer_dlc_stanleyparable/announcer_player_reconnect_03.mp3",
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
    };

    public AudioPlayer(Plugin plugin)
    {
        _pluginInterface = plugin.PluginInterface;
        _configuration = plugin.Configuration;

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

    public void UpdateVolume()
    {
        float targetVolume = 0.5f;

        try
        {
            if (_configuration.BindToXivVolumeSource)
            {
                uint baseVolume = XivUtility.GetVolume(_configuration.XivVolumeSource);
                uint masterVolume = XivUtility.GetVolume(XivVolumeSource.Master);
                uint baseVolumeBoost = _configuration.XivVolumeSourceBoost;

                targetVolume = Math.Clamp((baseVolume + baseVolumeBoost) * (masterVolume / 100f), 0, 100) / 100f;
            }
            else
            {
                targetVolume = Math.Clamp(_configuration.Volume, 0, 100) / 100f;
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
    }
    
    public void PlayRandomSoundFromCategory(AudioEvent @event)
    {
        PluginLog.Debug("Waiting for lock to play audio for {Event}", @event);
        
        lock (_lockObj)
        {
            if (_isPlaying) return;
        
            string lastPlayed = "";
            if (!_lastPlayed.ContainsKey(@event)) _lastPlayed[@event] = lastPlayed;
            else lastPlayed = _lastPlayed[@event];

            Random random = new();
            string[] choices = _audioEventMap[@event].Where(x => x != lastPlayed).ToArray();
            int index = random.Next(0, choices.Length);
            string result = choices[index];

            PlaySound(result);

            if (result == "announcer_dlc_stanleyparable/announcer_respawn_09.mp3") _adviceFollowUp = true;
            if (result == "announcer_dlc_stanleyparable/announcer_idle_09.mp3") _shrimpFactFollowUp = true;
            
            _lastPlayed[@event] = result;
        }
    }

    private void PlaySound(string resourcePath)
    {
        if (_isPlaying) return;
        
        string audioPath = Utility.GetResourcePath(_pluginInterface, resourcePath);
        PluginLog.Debug("Playing {ResourcePath}", resourcePath);
        
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