using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskHub.Abstractions;

namespace CcmExecHandler;

public class TriggerScheduleCommand : ICommand
{
    public TriggerScheduleCommand(TriggerScheduleRequest request, ILogger logger)
    {
        Request = request;
        _logger = logger;
    }

    public TriggerScheduleRequest Request { get; }

    private readonly ILogger _logger;

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken)
    {
        if (!CcmSchedules.TryGetScheduleId(Request.Task, out var scheduleId))
        {
            var error = JsonSerializer.SerializeToElement($"Unknown task '{Request.Task}'");
            return Task.FromResult(new OperationResult(error, "unknown-task"));
        }

        dynamic cm = service.GetService();
        var parameters = new Dictionary<string, object?> { ["sScheduleID"] = scheduleId };
        OperationResult result = cm.InvokeMethod(".", "root\\ccm", "SMS_Client", "TriggerSchedule", parameters);
        return Task.FromResult(result);
    }
}
