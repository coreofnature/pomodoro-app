using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<PomodoroEngine>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PomodoroEngine>());

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Endpoints
app.MapGet("/api/state", (PomodoroEngine engine) => Results.Ok(engine.GetState()));

app.MapPost("/api/start", (PomodoroEngine engine, StartRequest? req) =>
{
    engine.Start(req?.WorkMinutes ?? 25, req?.BreakMinutes ?? 5, req?.LongBreakMinutes ?? 15);
    return Results.Ok(engine.GetState());
});

app.MapPost("/api/pause", (PomodoroEngine engine) =>
{
    engine.TogglePause();
    return Results.Ok(engine.GetState());
});

app.MapPost("/api/skip", (PomodoroEngine engine) =>
{
    engine.Skip();
    return Results.Ok(engine.GetState());
});

app.MapPost("/api/reset", (PomodoroEngine engine) =>
{
    engine.Reset();
    return Results.Ok(engine.GetState());
});

app.MapPost("/api/configure", (PomodoroEngine engine, StartRequest req) =>
{
    engine.Configure(req.WorkMinutes, req.BreakMinutes, req.LongBreakMinutes);
    return Results.Ok(engine.GetState());
});

app.Run();

// Models & Engine
public record StartRequest(int WorkMinutes, int BreakMinutes, int LongBreakMinutes);

public enum SessionType { Work, ShortBreak, LongBreak }
public enum TimerStatus { Idle, Running, Paused }

public record PomodoroDto(
    string Status,
    string Session,
    int RemainingSeconds,
    int TotalSeconds,
    int CompletedCycles,
    int CurrentCycle
);

public class PomodoroEngine : BackgroundService
{
    private readonly object _lock = new();
    private TimerStatus _status = TimerStatus.Idle;
    private SessionType _session = SessionType.Work;

    private int _workSec = 25 * 60;
    private int _shortBreakSec = 5 * 60;
    private int _longBreakSec = 15 * 60;

    private int _remainingSec = 25 * 60;
    private int _totalSec = 25 * 60;
    private int _completedCycles = 0;
    private int _currentCycle = 1;

    public PomodoroDto GetState()
    {
        lock (_lock)
        {
            return new PomodoroDto(
                _status.ToString(),
                _session.ToString(),
                _remainingSec,
                _totalSec,
                _completedCycles,
                _currentCycle
            );
        }
    }

    public void Start(int workMin, int breakMin, int longBreakMin)
    {
        lock (_lock)
        {
            _workSec = workMin * 60;
            _shortBreakSec = breakMin * 60;
            _longBreakSec = longBreakMin * 60;

            if (_status == TimerStatus.Idle)
            {
                _session = SessionType.Work;
                _totalSec = _workSec;
                _remainingSec = _workSec;
                _currentCycle = 1;
            }
            _status = TimerStatus.Running;
        }
    }

    public void TogglePause()
    {
        lock (_lock)
        {
            if (_status == TimerStatus.Running) _status = TimerStatus.Paused;
            else if (_status == TimerStatus.Paused) _status = TimerStatus.Running;
        }
    }

    public void Skip()
    {
        lock (_lock)
        {
            TransitionNext();
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _status = TimerStatus.Idle;
            _session = SessionType.Work;
            _totalSec = _workSec;
            _remainingSec = _workSec;
            _currentCycle = 1;
        }
    }

    public void Configure(int workMin, int breakMin, int longBreakMin)
    {
        lock (_lock)
        {
            _workSec = workMin * 60;
            _shortBreakSec = breakMin * 60;
            _longBreakSec = longBreakMin * 60;

            if (_status == TimerStatus.Idle)
            {
                _session = SessionType.Work;
                _totalSec = _workSec;
                _remainingSec = _workSec;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            lock (_lock)
            {
                if (_status != TimerStatus.Running) continue;

                if (_remainingSec > 0)
                {
                    _remainingSec--;
                }
                else
                {
                    TransitionNext();
                }
            }
        }
    }

    private void TransitionNext()
    {
        if (_session == SessionType.Work)
        {
            _completedCycles++;
            if (_completedCycles % 4 == 0)
            {
                _session = SessionType.LongBreak;
                _totalSec = _longBreakSec;
            }
            else
            {
                _session = SessionType.ShortBreak;
                _totalSec = _shortBreakSec;
            }
        }
        else
        {
            _session = SessionType.Work;
            _totalSec = _workSec;
            _currentCycle++;
        }
        _remainingSec = _totalSec;
    }
}