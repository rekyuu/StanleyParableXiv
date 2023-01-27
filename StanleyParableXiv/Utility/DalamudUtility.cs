using System.IO;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Utility;

public static class DalamudUtility
{
    /// <summary>
    /// Fetches the full path of the supplied file from the Resources directory.
    /// </summary>
    /// <param name="pluginInterface">The Dalamud plugin interface.</param>
    /// <param name="resource">The path within the Resources folder.</param>
    /// <returns>The full path to the resource.</returns>
    public static string GetResourcePath(DalamudPluginInterface pluginInterface, string resource)
    {
        return Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, $"Resources/{resource}");
    }

    /// <summary>
    /// Gets the supplied party member as a player character.
    /// </summary>
    /// <param name="partyMember">The party member to get.</param>
    /// <returns>The party member as a player character object.</returns>
    public static PlayerCharacter? GetPlayerCharacterFromPartyMember(PartyMember partyMember)
    {
        uint objId = partyMember.ObjectId;
        GameObject? obj = DalamudService.ObjectTable.SearchById(objId);
            
        if (obj?.GetType() == typeof(PlayerCharacter)) return (obj as PlayerCharacter)!;

        return null;
    }
}