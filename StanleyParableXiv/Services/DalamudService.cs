using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace StanleyParableXiv.Services;

public class DalamudService
{
    public static void Initialize(DalamudPluginInterface pluginInterface) => pluginInterface.Create<DalamudService>();
    
    [PluginService]
    public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    
    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null!;
    
    [PluginService]
    public static IDataManager DataManager { get; private set; } = null!;
    
    [PluginService]
    public static IFramework Framework { get; private set; } = null!;
    
    [PluginService]
    public static IChatGui ChatGui { get; private set; } = null!;
    
    [PluginService]
    public static IGameNetwork GameNetwork { get; private set; } = null!;
    
    [PluginService]
    public static ICondition Condition { get; private set; } = null!;
    
    [PluginService]
    public static IClientState ClientState { get; private set; } = null!;
    
    [PluginService]
    public static IPartyList PartyList { get; private set; } = null!;
    
    [PluginService]
    public static IObjectTable ObjectTable { get; private set; } = null!;
    
    [PluginService]
    public static IDutyState DutyState { get; private set; } = null!;
    
    [PluginService]
    public static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
}