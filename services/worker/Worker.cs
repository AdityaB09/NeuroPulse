using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker online — waiting for DAG jobs...");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(2000, stoppingToken);
        }
    }
}
