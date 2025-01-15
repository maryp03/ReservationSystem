using Microsoft.EntityFrameworkCore;
using ReservationSystem.Data;

public class ReservationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public ReservationCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ReservationSystemContext>();
                var now = DateTime.Now; 
                var cutoffTime = now.AddHours(-1);

                var oldReservations = await context.Reservations
                    .Where(r => r.ReservationTime < cutoffTime)
                    .ToListAsync();

                if (oldReservations.Any())
                {
                    context.Reservations.RemoveRange(oldReservations);
                    await context.SaveChangesAsync();
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
