using System;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Condition = Dalamud.Game.ClientState.Conditions.Condition;
using Status = FFXIVClientStructs.FFXIV.Client.Game.Status;

namespace StanleyParableXiv
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Stanley Parable XIV";
        
        public Configuration Configuration { get; init; }
        
        public DalamudPluginInterface PluginInterface { get; init; }
        
        public AudioPlayer AudioPlayer { get; private set; }

        private uint _lastXivVolumeSource = 0;
        private uint _lastXivMasterVolume = 0;
        
        private IntPtr _afkTimerBaseAddress = IntPtr.Zero;
        private delegate long AfkTimerHookDelegate(IntPtr a1, float a2);
        private readonly Hook<AfkTimerHookDelegate>? _afkTimerHook;
        private TimerService? _afkTimerService;
        private bool _afkPlayed = false;
        
        private IntPtr _countdownTimerBaseAddress = IntPtr.Zero;
        private delegate IntPtr CountdownTimerHookDelegate(ulong p1);
        private readonly Hook<CountdownTimerHookDelegate>? _countdownTimerHook;
        private bool _countdownStartPlayed = false;
        private bool _countdown10Played = false;

        private bool _wasDead = false;
        private bool _wasBoundByDuty = false;

        private readonly WindowSystem _windowSystem = new("StanleyParableXiv");

        private readonly CommandManager _commandManager;
        private readonly DataManager _dataManager;
        private readonly Framework _framework;
        private readonly ChatGui _chatGui;
        private readonly GameNetwork _gameNetwork;
        private readonly Condition _condition;
        private readonly SigScanner _sigScanner;
        private readonly ClientState _clientState;

        [Obsolete("Obsolete")]
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] GameNetwork gameNetwork,
            [RequiredVersion("1.0")] Condition condition,
            [RequiredVersion("1.0")] SigScanner sigScanner,
            [RequiredVersion("1.0")] ClientState clientState)
        {
            PluginLog.Information("Starting plugin");
            
            PluginInterface = pluginInterface;
            
            _commandManager = commandManager;
            _dataManager = dataManager;
            _framework = framework;
            _chatGui = chatGui;
            _gameNetwork = gameNetwork;
            _condition = condition;
            _sigScanner = sigScanner;
            _clientState = clientState;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            
            AudioPlayer = new AudioPlayer(this);

            _windowSystem.AddWindow(new ConfigWindow(this));

            _commandManager.AddHandler("/narrator", new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Stanley Parable XIV configuration."
            });

            _commandManager.AddHandler("/narratorvolume", new CommandInfo(OnVolumeCommand)
            {
                HelpMessage = "Sets the volume for the Narrator."
            });

            PluginInterface.UiBuilder.Draw += DrawUi;

            _framework.Update += OnFrameworkUpdate;
            _chatGui.ChatMessage += OnChatMessage;
            _gameNetwork.NetworkMessage += OnGameNetworkMessage;

            _afkTimerHook = Hook<AfkTimerHookDelegate>.FromAddress(
                _sigScanner.ScanText("48 8B C4 48 89 58 18 48 89 70 20 55 57 41 55"),
                OnAfkTimerHook);
            _afkTimerHook.Enable();

            _countdownTimerHook = Hook<CountdownTimerHookDelegate>.FromAddress(
                _sigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41"),
                OnCountdownTimerHook);
            _countdownTimerHook.Enable();

            _clientState.Login += OnLogin;

            CheckIfPlayerIsJustRespawned();
        }

        public void Dispose()
        {
            AudioPlayer.Dispose();
            
            _windowSystem.RemoveAllWindows();
            _commandManager.RemoveHandler("/narrator");
            _commandManager.RemoveHandler("/narratorvolume");

            PluginInterface.UiBuilder.Draw -= DrawUi;
            _framework.Update -= OnFrameworkUpdate;
            _chatGui.ChatMessage -= OnChatMessage;
            _gameNetwork.NetworkMessage -= OnGameNetworkMessage;

            DisposeAfkTimerHook();
            _afkTimerService?.Stop();
            _afkTimerService?.Dispose();
            
            switch (_countdownTimerHook)
            {
                case { IsDisposed: true }:
                    return;
                case { IsEnabled: true }:
                    _countdownTimerHook.Disable();
                    break;
            }

            _countdownTimerHook?.Dispose();

            _clientState.Login -= OnLogin;
        }

        private void OnConfigCommand(string command, string commandArgs)
        {
            _windowSystem.GetWindow("Stanley Parable XIV Configuration")!.IsOpen = true;
        }

        private void OnVolumeCommand(string command, string commandArgs)
        {
            try
            {
                uint volumeSetting = 0;
                string[] args = commandArgs.Split(" ");
                
                if (args.Length >= 1 && !string.IsNullOrEmpty(args[0])) volumeSetting = uint.Parse(args[0]);
                if (volumeSetting is < 0 or > 100) throw new Exception("Volume must be between 0 and 100");
                
                Configuration.Volume = volumeSetting;
                Configuration.BindToXivVolumeSource = false;
                Configuration.Save();
            
                AudioPlayer.UpdateVolume();
                
                _chatGui.Print($"Narrator volume set to {volumeSetting}.");
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Exception occurred while setting volume via command");
                _chatGui.PrintError($"\"{commandArgs}\" is not a valid setting.");
            }
        }

        private void DrawUi()
        {
            _windowSystem.Draw();
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            CheckIfPlayerIsJustRespawned();
            CheckIfPlayerIsJustBoundByDuty();
            
            if (!Configuration.BindToXivVolumeSource) return;

            uint nextVolumeSource = XivUtility.GetVolume(Configuration.XivVolumeSource);
            uint nextMasterVolume = XivUtility.GetVolume(XivVolumeSource.Master);
            
            if (_lastXivVolumeSource == nextVolumeSource && _lastXivMasterVolume == nextMasterVolume) return;
            
            PluginLog.Debug("Updating volume due to framework update");
            AudioPlayer.UpdateVolume();
            
            _lastXivVolumeSource = nextVolumeSource;
            _lastXivMasterVolume = nextMasterVolume;
        }

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message,
            ref bool isHandled)
        {
            string synthesisFailedMessage = _dataManager.GetExcelSheet<LogMessage>()!.GetRow(1160)!.Text.ToDalamudString().TextValue;

            // Synthesis Failed
            if (message.TextValue.Contains(synthesisFailedMessage))
            {
                AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Failure);
            }
        }

        private unsafe void OnGameNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId,
            NetworkMessageDirection direction)
        {
            var cat = *(ushort*)(dataPtr + 0x00);
            var updateType = *(uint*)(dataPtr + 0x08);
            
            #if DEBUG
            PluginLog.Verbose("opCode = {OpCode}, cat = {Cat}, updateType = {UpdateType}", opCode, cat.ToString("X"), updateType.ToString("X"));
            #endif
            
            // Marketboard Purchase
            if (opCode == _dataManager.ServerOpCodes["MarketBoardPurchase"])
            {
                AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.MarketboardPurchase);
            }
            
            if (opCode != _dataManager.ServerOpCodes["ActorControlSelf"]) return;

            switch (cat)
            {
                // Encounter Complete
                case 0x6D when updateType == 0x40000003:
                    Task.Delay(1000).ContinueWith(t =>
                    {
                        AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.EncounterComplete);
                    });
                    break;
                // Encounter Start
                case 0x6D when updateType == 0x40000001:
                    AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.EncounterStart);
                    break;
            }
        }

        private unsafe long OnAfkTimerHook(IntPtr a1, float a2)
        {
            _afkTimerBaseAddress = a1;
            PluginLog.Debug($"Found AFK timer base address: {_afkTimerBaseAddress:X16}");

            _afkTimerService = new TimerService(30, () =>
            {
                if (_afkPlayed) return;
                
                float* afkTimer1 = (float*)(_afkTimerBaseAddress + 20);
                float* afkTimer2 = (float*)(_afkTimerBaseAddress + 24);
                float* afkTimer3 = (float*)(_afkTimerBaseAddress + 28);
            
                #if DEBUG
                PluginLog.Verbose($"AFK Timers = {*afkTimer1}/{*afkTimer2}/{*afkTimer3}");
                #endif
                
                if (new[] { *afkTimer1, *afkTimer2, *afkTimer3 }.Max() > 300f)
                {
                    AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Afk);
                    _afkPlayed = true;
                }
                else _afkPlayed = false;
            });
            
            _afkTimerService.Start();
            DisposeAfkTimerHook();

            return 0;
        }

        private void DisposeAfkTimerHook()
        {
            switch (_afkTimerHook)
            {
                case { IsDisposed: true }:
                    return;
                case { IsEnabled: true }:
                    _afkTimerHook.Disable();
                    break;
            }

            _afkTimerHook?.Dispose();
            
            PluginLog.Debug("AFK timer hook disposed.");
        }

        private IntPtr OnCountdownTimerHook(ulong value)
        {
            if (value != 0)
            {
                float countdownValue = Marshal.PtrToStructure<float>((IntPtr)value + 0x2c);
                
                #if DEBUG
                PluginLog.Verbose("Countdown Timer hook value = {CountdownValue}", countdownValue);
                #endif
                
                if (countdownValue < 0f)
                {
                    _countdownStartPlayed = false;
                    _countdown10Played = false;
            
                    return _countdownTimerHook!.Original(value);
                }

                if (!_countdownStartPlayed)
                {
                    AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.CountdownStart);
                    _countdownStartPlayed = true;
                }

                if (_countdownStartPlayed && !_countdown10Played && countdownValue < 10f)
                {
                    AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Countdown10);
                    _countdown10Played = true;
                }
            }
            
            return _countdownTimerHook!.Original(value);
        }

        private void OnLogin(object? sender, EventArgs eventArgs)
        {
            AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Login);
        }

        private void CheckIfPlayerIsJustRespawned()
        {
            var player = _clientState.LocalPlayer;
            if (player == null) return;

            if (_wasDead && !player.IsDead)
            {
                AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Respawn);    
            }
            
            _wasDead = player.IsDead;
        }

        private void CheckIfPlayerIsJustBoundByDuty()
        {
            bool isBoundByDuty = _condition[ConditionFlag.BoundByDuty] ||
                                 _condition[ConditionFlag.BoundByDuty56] ||
                                 _condition[ConditionFlag.BoundByDuty95];

            // Ignore Island Sanctuary
            TerritoryType? territory = _dataManager.Excel.GetSheet<TerritoryType>()?.GetRow(_clientState.TerritoryType);
            isBoundByDuty = isBoundByDuty && territory?.TerritoryIntendedUse != 49;

            if (_wasBoundByDuty && !isBoundByDuty)
            {
                AudioPlayer.PlayRandomSoundFromCategory(AudioEvent.Failure);
            }
            
            _wasBoundByDuty = isBoundByDuty;
        }
    }
}
