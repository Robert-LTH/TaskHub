using System;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace IntuneManagementExtensionHandler;

public class TriggerSyncCommand : ICommand
{
    public TriggerSyncCommand(SyncRequest request)
    {
        Request = request;
    }

    public SyncRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken, ClientWebSocket? socket = null)
    {
        const string script = @"
$svc = Get-Service -Name 'IntuneManagementExtension' -ErrorAction SilentlyContinue
if (-not $svc) { 'not-installed'; return }
if ($svc.Status -eq 'Running') { Restart-Service -Name 'IntuneManagementExtension' }
else { Start-Service -Name 'IntuneManagementExtension' }
(Get-Service -Name 'IntuneManagementExtension').Status
";
        var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(script));
        dynamic ps = service.GetService();
        OperationResult result = ps.Execute(scriptBase64);
        return Task.FromResult(result);
    }
}
