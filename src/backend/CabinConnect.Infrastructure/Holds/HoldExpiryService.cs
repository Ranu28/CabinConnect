using CabinConnect.Domain.Holds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CabinConnect.Infrastructure.Holds;

public sealed class HoldExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryService> _logger;
    private readonly TimeSpan _interval;

    public HoldExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        var minutes   = int.TryParse(config["HoldExpiryIntervalMinutes"], out var m) ? m : 5;
        _interval     = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunIterationAsync(stoppingToken);
    }

    // Internal so tests can drive a single iteration without the PeriodicTimer.
    internal async Task RunIterationAsync(CancellationToken ct = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo  = scope.ServiceProvider.GetRequiredService<IHoldRepository>();
            var count = await repo.CancelExpiredHoldsAsync(ct);
            _logger.LogInformation("Hold expiry job: {Count} hold(s) cancelled", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hold expiry job failed; will retry on next tick");
        }
    }
}
