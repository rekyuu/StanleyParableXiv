using System;
using System.Threading;
using System.Threading.Tasks;

namespace StanleyParableXiv;

public class TimerService : IDisposable
{
    private Timer? _timer;
    private readonly uint _timerSeconds;
    private readonly Action _method;

    public TimerService(uint timerSeconds, Action method)
    {
        _timerSeconds = timerSeconds;
        _method = method;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }

    public void Start()
    {
        _timer = new Timer(RunMethod, null, TimeSpan.Zero, TimeSpan.FromSeconds(_timerSeconds));
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, 0);
    }

    private void RunMethod(object? state)
    {
        Task.Run(() => _method());
    }
}