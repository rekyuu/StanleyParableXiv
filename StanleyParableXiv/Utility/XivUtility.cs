using System;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Memory;

namespace StanleyParableXiv.Utility;

public enum XivVolumeSource
{
    Bgm,
    Se,
    Voice,
    Env,
    System,
    Perform,
    Master
}

public static class XivUtility
{
    private static readonly Dictionary<XivVolumeSource, string> XivVolumeSourceMap = new()
    {
        { XivVolumeSource.Bgm, "SoundBgm" },
        { XivVolumeSource.Se, "SoundSe" },
        { XivVolumeSource.Voice, "SoundVoice" },
        { XivVolumeSource.Env, "SoundEnv" },
        { XivVolumeSource.System, "SoundSystem" },
        { XivVolumeSource.Perform, "SoundPerform" },
        { XivVolumeSource.Master, "SoundMaster" },
    };

    public static unsafe uint GetVolume(XivVolumeSource soundType)
    {
        string volumeSourceName = XivVolumeSourceMap[soundType];
        
        try
        {
            var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
            var configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;

            for (int i = 0; i < configBase.ConfigCount; i++)
            {
                var entry = configBase.ConfigEntry[i];
                if (entry.Name == null) continue;

                string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));
                if (name != volumeSourceName) continue;

                return entry.Value.UInt;
            }

            PluginLog.Error("Unable to find config value for {VolumeSource}", volumeSourceName);
            return 50;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "An exception occurred while obtaining volume");
            return 50;
        }
    }
}