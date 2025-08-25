using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using TaskHub.Abstractions;

namespace CcmExecHandler;

public class TriggerScheduleCommand : ICommand
{
    public TriggerScheduleCommand(TriggerScheduleRequest request)
    {
        Request = request;
    }

    public TriggerScheduleRequest Request { get; }

    public Task<OperationResult> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken, ClientWebSocket? socket = null)
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
