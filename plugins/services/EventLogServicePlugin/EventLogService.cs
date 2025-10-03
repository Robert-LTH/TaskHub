using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Text.Json;
using TaskHub.Abstractions;

namespace EventLogServicePlugin;

public class EventLogServicePlugin : IServicePlugin
{
    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "eventlog";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

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

        public OperationResult Read(string logName = "Application", string? xpath = null)
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return new OperationResult(null, "EventLog is only supported on Windows");
                }

                var events = new List<string>();
                EventLogQuery query = xpath is null
                    ? new EventLogQuery(logName, PathType.LogName)
                    : new EventLogQuery(logName, PathType.LogName, xpath);

                using EventLogReader reader = new EventLogReader(query);
                for (EventRecord? record = reader.ReadEvent(); record != null; record = reader.ReadEvent())
                {
                    using (record)
                    {
                        events.Add(record.ToXml());
                    }
                }

                return new OperationResult(JsonSerializer.SerializeToElement(events), "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to read event log: {ex.Message}");
            }
        }
    }
}

