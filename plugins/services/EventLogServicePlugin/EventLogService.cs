using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using TaskHub.Abstractions;

namespace EventLogServicePlugin;

public class EventLogServicePlugin : IServicePlugin
{
    public string Name => "eventlog";

    public object GetService() => new EventLogService();

    public class EventLogService
    {
        public OperationResult Write(string source, string message, string logName = "Application", string entryType = "Information")
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new OperationResult(null, "EventLog is only supported on Windows");
                }

                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }

                if (!Enum.TryParse<EventLogEntryType>(entryType, true, out var type))
                {
                    type = EventLogEntryType.Information;
                }

                EventLog.WriteEntry(source, message, type);
                return new OperationResult(JsonSerializer.SerializeToElement(message), "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to write event: {ex.Message}");
            }
        }
    }
}
