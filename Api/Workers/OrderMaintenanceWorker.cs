using Application.Features.Orders;
using Application.Mediator;

namespace Api.Workers;

/// <summary>Every 30 seconds: delay alerts, admin escalation and expiry of unpaid transfer orders.</summary>
public sealed class OrderMaintenanceWorker(IServiceScopeFactory scopes, ILogger<OrderMaintenanceWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var changed = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new RunOrderMaintenanceCommand(), stoppingToken);
                if (changed > 0) logger.LogInformation("Order maintenance: {Count} order(s) alerted or expired", changed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Order maintenance failed; retrying on the next tick");
            }
        }
    }
}
