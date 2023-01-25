using System;
using StanleyParableXiv.Events;

namespace StanleyParableXiv.Services;

public class EventService : IDisposable
{
    private readonly AfkEvent _afkEvent;
    private readonly CountdownEvent _countdownEvent;
    private readonly DutyEvent _dutyEvent;
    private readonly LoginEvent _loginEvent;
    private readonly MarketBoardPurchaseEvent _marketBoardPurchaseEvent;
    private readonly PlayerDeathEvent _playerDeathEvent;
    private readonly PvpEvent _pvpEvent;
    private readonly SynthesisFailedEvent _synthesisFailedEvent;
    
    public EventService()
    {
        _afkEvent = new AfkEvent();
        _countdownEvent = new CountdownEvent();
        _dutyEvent = new DutyEvent();
        _loginEvent = new LoginEvent();
        _marketBoardPurchaseEvent = new MarketBoardPurchaseEvent();
        _playerDeathEvent = new PlayerDeathEvent();
        _pvpEvent = new PvpEvent();
        _synthesisFailedEvent = new SynthesisFailedEvent();
    }
    
    public void Dispose()
    {
        _afkEvent.Dispose();
        _countdownEvent.Dispose();
        _dutyEvent.Dispose();
        _loginEvent.Dispose();
        _marketBoardPurchaseEvent.Dispose();
        _playerDeathEvent.Dispose();
        _pvpEvent.Dispose();
        _synthesisFailedEvent.Dispose();
    }
}