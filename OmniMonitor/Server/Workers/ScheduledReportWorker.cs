public class ScheduledReportWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledReportWorker> _logger;

    public ScheduledReportWorker(IServiceProvider serviceProvider, ILogger<ScheduledReportWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledReportWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();

                await reportService.ProcessScheduledReportsAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ScheduledReportWorker.");
            }

            // correr cada X tiempo (ej 1 minuto)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
