# Improvement: BackgroundService Must Use IServiceScopeFactory for Scoped Dependencies

**Date:** 2026-06-15
**Triggered by:** [BOLT-002 Retro](../retros/2026-06-15-bolt-002-retro.md)
**Status:** Proposed

---

## Problem

`HoldExpiryService` initially injected `NpgsqlDataSource` (singleton) directly into its constructor. When refactored to use `IHoldRepository` (scoped), the same direct injection pattern would have caused a captive dependency bug: a singleton holding a reference to a scoped service, which is invalid in .NET DI.

More practically: the `NpgsqlDataSource` singleton's factory lambda throws when the connection string is absent (the production safety guard). Because `BackgroundService` is a singleton and is constructed at host startup, this throw fired during every integration test boot — causing all 23 API tests to fail before a single assertion ran.

The fix — using `IServiceScopeFactory` to create a per-tick scope and resolve `IHoldRepository` within it — is the correct .NET pattern for background services that need scoped services. It also defers DB access to the timer loop, where it is guarded by try/catch.

## Rule

> Any `BackgroundService` that needs to access the database or call a scoped service **must** inject `IServiceScopeFactory` and create a new scope per execution interval. It must never inject `NpgsqlDataSource` or any scoped service directly.

## Applies To

- All classes inheriting `BackgroundService` or implementing `IHostedService`
- Review checklist: add as a check for any PR introducing a new hosted service

## Pattern

```csharp
public sealed class MyBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MyBackgroundService(IServiceScopeFactory scopeFactory, ...)
        => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IMyRepository>();
                await repo.DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job failed; will retry on next tick");
            }
        }
    }
}
```
