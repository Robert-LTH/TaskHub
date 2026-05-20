using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace ProcessHandler;

public class StartProcessCommand : ICommand
{
    public StartProcessCommand(StartProcessRequest request)
    {
        Request = request;
    }

    public StartProcessRequest Request { get; }

    public async Task<OperationResult> ExecuteAsync(IServicePlugin service, ILogger logger, CancellationToken cancellationToken)
    {
        dynamic process = service.GetService();
        OperationResult result = await process.StartAsync(
            Request.FileName,
            Request.Arguments,
            Request.ArgumentList,
            Request.WorkingDirectory,
            Request.Environment,
            Request.TimeoutMilliseconds,
            cancellationToken);

        if (result.Payload.HasValue)
        {
            var payload = result.Payload.Value;
            var exitCode = payload.TryGetProperty("exitCode", out var exitCodeElement) && exitCodeElement.ValueKind != System.Text.Json.JsonValueKind.Null
                ? exitCodeElement.GetInt32().ToString()
                : "none";
            logger.LogInformation("Process {FileName} completed with exit code {ExitCode}", Request.FileName, exitCode);
        }
        else
        {
            logger.LogWarning("Process {FileName} did not return a payload: {Result}", Request.FileName, result.Result);
        }

        return result;
    }
}
