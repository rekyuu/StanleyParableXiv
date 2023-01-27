using System;
using System.Collections.Generic;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;

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
        { XivVolumeSource.Bgm, "Bgm" },
        { XivVolumeSource.Se, "Se" },
        { XivVolumeSource.Voice, "Voice" },
        { XivVolumeSource.Env, "Env" },
        { XivVolumeSource.System, "System" },
        { XivVolumeSource.Perform, "Perform" },
        { XivVolumeSource.Master, "Master" },
    };

    public static unsafe uint GetVolume(XivVolumeSource soundType)
    {
        string volumeSourceName = XivVolumeSourceMap[soundType];
        string volumeSourceAmountKey = $"Sound{volumeSourceName}";
        string volumeSourceMutedKey = $"IsSnd{volumeSourceName}";

        uint? volumeAmount = null;
        bool? volumeMuted = null;
        
        try
        {
            Framework* framework = Framework.Instance();
            ConfigBase configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;

            for (int i = 0; i < configBase.ConfigCount; i++)
            {
                ConfigEntry entry = configBase.ConfigEntry[i];
                if (entry.Name == null) continue;

                string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));

                if (name == volumeSourceAmountKey) volumeAmount = entry.Value.UInt;
                else if (name == volumeSourceMutedKey) volumeMuted = entry.Value.UInt == 1;
            }

            if (volumeAmount == null || volumeMuted == null)
            {
                throw new Exception($"Unable to determine volume for {volumeSourceName}");
            }

            return volumeMuted.Value ? 0 : volumeAmount.Value;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "An exception occurred while obtaining volume");
            return 50;
        }
    }
}