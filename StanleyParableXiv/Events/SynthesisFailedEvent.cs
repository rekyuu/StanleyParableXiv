using System;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using StanleyParableXiv.Services;

namespace StanleyParableXiv.Events;

public class SynthesisFailedEvent : IDisposable
{
    private readonly string _synthesisFailedMessage;
    
    public SynthesisFailedEvent()
    {
        _synthesisFailedMessage = DalamudService.DataManager.GetExcelSheet<LogMessage>()!.GetRow(1160)!.Text.ToDalamudString().TextValue;

        DalamudService.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        DalamudService.ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (message.TextValue.Contains(_synthesisFailedMessage) && Configuration.Instance.EnableSynthesisFailedEvent)
        {
            AudioPlayer.Instance.PlayRandomSoundFromCategory(AudioEvent.Failure);
        }
    }
}