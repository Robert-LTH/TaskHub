using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;
using Microsoft.Extensions.Logging;

namespace PowerShellHandler;

public class PowerShellCommand : ICommand
{
    private readonly ILogger _logger;

    public PowerShellCommand(PowerShellScriptRequest request, ILogger logger)
    {
        Request = request;
        _logger = logger;
    }

    public PowerShellScriptRequest Request { get; }

    Task<OperationResult> ICommand.ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing PowerShell script ({Length} chars)", Request.Script?.Length ?? 0);
        var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Request.Script));
        dynamic ps = service.GetService();
        OperationResult result = ps.Execute(scriptBase64, Request.Version, Request.Properties);
        if (!string.Equals(result.Result, "success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("PowerShell script failed: {Result}", result.Result);
        }
        return Task.FromResult(result);
    }
}
