using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace PowerShellHandler;

public class PowerShellCommand : ICommand
{
    public PowerShellCommand(PowerShellScriptRequest request)
    {
        Request = request;
    }

    public PowerShellScriptRequest Request { get; }

    Task<OperationResult> ICommand.ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Request.Script));
        dynamic ps = service.GetService();
        OperationResult result = ps.Execute(scriptBase64, Request.Version, Request.Properties);
        return Task.FromResult(result);
    }
}
