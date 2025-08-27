using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace TaskHub.Server;

/// <summary>
/// Background service that periodically sends accumulated inventory reports
/// to a SignalR hub.
/// </summary>
public class ReportingService : BackgroundService
{
    private readonly IReportingContainer _container;
    private readonly ILogger<ReportingService> _logger;
    private readonly IConfiguration _configuration;
    private HubConnection? _connection;

    public ReportingService(IReportingContainer container, IConfiguration configuration, ILogger<ReportingService> logger)
    {
        _container = container;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = _configuration["Reporting:HubUrl"];
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("Reporting hub URL is not configured.");
            return;
        }

        _connection = new HubConnectionBuilder().WithUrl(url).Build();
        await _connection.StartAsync(stoppingToken);

        var intervalSeconds = _configuration.GetValue("Reporting:IntervalSeconds", 30);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var reports = _container.DrainReports();
                if (reports.Count > 0)
                {
                    await _connection.SendAsync("ReportInventory", reports, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send inventory reports");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
