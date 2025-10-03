using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OverviewServicePlugin;
using SysManServicePlugin;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace OverviewSyncHandler;

public class SyncOneCommand : ICommand
{
    private readonly SysManClient _sysManService;
    private readonly OverviewApiClient _overviewService;
    private readonly SqlServicePlugin.SqlServicePlugin.SqlServiceRegistry _sqlRegistry;

    public SyncOneCommand(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

        var sysManPlugin = serviceProvider.GetRequiredService<SysManServicePlugin.SysManServicePlugin>();
        var overviewPlugin = serviceProvider.GetRequiredService<OverviewServicePlugin.OverviewServicePlugin>();
        var sqlPlugin = serviceProvider.GetRequiredService<SqlServicePlugin.SqlServicePlugin>();

        _sysManService = (SysManClient)sysManPlugin.GetService();
        _overviewService = (OverviewApiClient)overviewPlugin.GetService();
        _sqlRegistry = (SqlServicePlugin.SqlServicePlugin.SqlServiceRegistry)sqlPlugin.GetService();
    }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        var releasesPayload = await _overviewService.GetReleasesAsync(cancellationToken);
        var releases = JsonSerializer.SerializeToElement(releasesPayload);
        var deleteTagResult = await _sqlRegistry["overview"].DeleteAsync("DELETE * FROM sysman_tag");
        var deleteTargetTagResult = await _sqlRegistry["overview"].DeleteAsync("DELETE * FROM sysman_target_tag");
        logger.LogInformation(releases.GetRawText());

        return new OperationResult(releases, "success");
    }
}
