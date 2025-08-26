using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace PowerShellHandler;

public class PowerShellCommand : ICommand
{
    private readonly IServicePlugin _service;

    public PowerShellCommand(IServicePlugin service, PowerShellScriptRequest request)
    {
        _service = service;
        Request = request;
    }

    public PowerShellScriptRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var scriptBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Request.Script));
        dynamic ps = _service.GetService();
        OperationResult result = ps.Execute(scriptBase64, Request.Version, Request.Properties);
        return Task.FromResult(result);
    }

    Task<OperationResult> ICommand.ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
        => ExecuteAsync(cancellationToken);
}
