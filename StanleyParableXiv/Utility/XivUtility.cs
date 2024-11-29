using System;
using System.Collections.Generic;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Lumina.Data;
using Lumina.Excel.Sheets;
using StanleyParableXiv.Services;

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

public static unsafe class XivUtility
{
    private static readonly SystemConfig ConfigBase = Framework.Instance()->SystemConfig.SystemConfigBase;
    
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

    private static readonly Dictionary<string, int> SoundConfigMap = [];

    static XivUtility()
    {
        InitializeSoundConfigMap();
    }

    /// <summary>
    /// Gets the volume amount of the supplied FFXIV channel.
    /// </summary>
    /// <param name="soundType">The FFXIV volume channel.</param>
    /// <returns>The volume amount between 0-100.</returns>
    /// <exception cref="Exception">Will be thrown if the supplied channel is not found.</exception>
    public static uint GetVolume(XivVolumeSource soundType)
    {
        string volumeSourceName = XivVolumeSourceMap[soundType];
        string volumeSourceAmountKey = $"Sound{volumeSourceName}";
        string volumeSourceMutedKey = $"IsSnd{volumeSourceName}";

        try
        {
            uint? volumeAmount = GetVolumeForConfigEntry(SoundConfigMap[volumeSourceAmountKey]);
            bool? volumeMuted = GetIsMutedForConfigEntry(SoundConfigMap[volumeSourceMutedKey]);

            if (volumeAmount == null || volumeMuted == null)
            {
                throw new Exception($"Unable to determine volume for {volumeSourceName}");
            }

            return volumeMuted.Value ? 0 : volumeAmount.Value;
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "An exception occurred while obtaining volume");
            return 50;
        }
    }

    private static void InitializeSoundConfigMap()
    {
        for (int i = 0; i < ConfigBase.ConfigCount; i++)
        {
            ConfigEntry entry = ConfigBase.ConfigEntry[i];
            if (entry.Name == null) continue;

            string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));

            if (name.StartsWith("Sound") || name.StartsWith("IsSnd")) SoundConfigMap.Add(name, i);
        }
    }

    private static uint? GetVolumeForConfigEntry(int index)
    {
        return ConfigBase.ConfigEntry[index].Value.UInt;
    }

    private static bool? GetIsMutedForConfigEntry(int index)
    {
        return ConfigBase.ConfigEntry[index].Value.UInt == 1;
    }

    /// <summary>
    /// Checks if the territory is Unreal, Extreme, Savage, or Ultimate difficulty.
    /// </summary>
    /// <param name="territoryType">The territory to check against.</param>
    /// <returns>True if high-end, false otherwise.</returns>
    public static bool TerritoryIsHighEndDuty(ushort territoryType)
    {
        bool territoryExists = DalamudService.DataManager.Excel
            .GetSheet<TerritoryType>(Language.English)
            .TryGetRow(territoryType, out TerritoryType territory);

        if (!territoryExists) return false;

        string name = territory
            .ContentFinderCondition.Value
            .Name
            .ToString();

        bool isHighEndDuty = name.StartsWith("the Minstrel's Ballad")
            || name.EndsWith("(Unreal)")
            || name.EndsWith("(Extreme)")
            || name.EndsWith("(Savage)")
            || name.EndsWith("(Ultimate)");
        
        DalamudService.Log.Debug("{DutyName} is high end: {IsHighEnd}", name, isHighEndDuty);

        return isHighEndDuty;
    }

    /// <summary>
    /// Checks if player's current territory is Unreal, Extreme, Savage, or Ultimate difficulty.
    /// </summary>
    /// <returns>True if high-end, false otherwise.</returns>
    public static bool PlayerIsInHighEndDuty()
    {
        return TerritoryIsHighEndDuty(DalamudService.ClientState.TerritoryType);
    }
}