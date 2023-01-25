using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using Condition = Dalamud.Game.ClientState.Conditions.Condition;

namespace StanleyParableXiv.Services;

public class DalamudService
{
    public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudService>();
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static CommandManager CommandManager { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static DataManager DataManager { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static Framework Framework { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static ChatGui ChatGui { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static GameNetwork GameNetwork { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static Condition Condition { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static SigScanner SigScanner { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static ClientState ClientState { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static PartyList PartyList { get; private set; } = null!;
    
    [PluginService]
    [RequiredVersion("1.0")]
    public static ObjectTable ObjectTable { get; private set; } = null!;
}