using System;
using StanleyParableXiv.Events;

namespace StanleyParableXiv.Services;

public class EventService : IDisposable
{
    private readonly AfkEvent _afkEvent = new();
    private readonly CountdownEvent _countdownEvent = new();
    private readonly DebugEvent _debugEvent = new();
    private readonly DutyEvent _dutyEvent = new();
    private readonly LoginEvent _loginEvent = new();
    private readonly MarketBoardPurchaseEvent _marketBoardPurchaseEvent = new();
    private readonly PlayerDeathEvent _playerDeathEvent = new();
    private readonly PvpEvent _pvpEvent = new();
    private readonly SynthesisFailedEvent _synthesisFailedEvent = new();

    public void Dispose()
    {
        _afkEvent.Dispose();
        _countdownEvent.Dispose();
        _debugEvent.Dispose();
        _dutyEvent.Dispose();
        _loginEvent.Dispose();
        _marketBoardPurchaseEvent.Dispose();
        _playerDeathEvent.Dispose();
        _pvpEvent.Dispose();
        _synthesisFailedEvent.Dispose();
        
        GC.SuppressFinalize(this);
    }
}