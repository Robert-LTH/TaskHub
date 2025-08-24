using System;
using System.Text.Json;

namespace TaskHub.Server;

public record RecurringCommandChainRequest(string[] Commands, JsonElement Payload, string CronExpression, TimeSpan Delay);

