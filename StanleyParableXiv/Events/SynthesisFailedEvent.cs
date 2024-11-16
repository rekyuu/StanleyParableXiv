using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class SynthesisFailedEvent : IDisposable
{
    private readonly string? _synthesisFailedMessage;
    
    /// <summary>
    /// Fires an event on crafting failure.
    /// Referenced from https://github.com/Roselyyn/EldenRingDalamud
    /// </summary>
    public SynthesisFailedEvent()
    {
        bool rowExists = DalamudService.DataManager.GetExcelSheet<LogMessage>().TryGetRow(1160, out LogMessage message);
        if (rowExists) _synthesisFailedMessage = message.Text.ToDalamudString().TextValue;

        DalamudService.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        DalamudService.ChatGui.ChatMessage -= OnChatMessage;
        GC.SuppressFinalize(this);
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool ishandled)
    {
        if (string.IsNullOrEmpty(_synthesisFailedMessage)) return;

        if (message.TextValue.Contains(_synthesisFailedMessage) && Configuration.Instance.EnableSynthesisFailedEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
        }
    }
}