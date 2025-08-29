using System.Collections.Generic;

namespace CcmExecHandler;

internal static class CcmSchedules
{
    private static readonly Dictionary<string, string> ScheduleIds = new()
    {
        ["machine-policy"] = "{00000000-0000-0000-0000-000000000021}",
        ["user-policy"] = "{00000000-0000-0000-0000-000000000027}",
        ["hardware-inventory"] = "{00000000-0000-0000-0000-000000000001}",
        ["software-inventory"] = "{00000000-0000-0000-0000-000000000002}",
        ["discovery-data"] = "{00000000-0000-0000-0000-000000000003}",
        ["app-deploy-eval"] = "{00000000-0000-0000-0000-000000000121}",
        ["software-update-scan"] = "{00000000-0000-0000-0000-000000000113}",
    };

    public static bool TryGetScheduleId(string task, out string id)
    {
        if (ScheduleIds.TryGetValue(task.ToLowerInvariant(), out var value))
        {
            id = value;
            return true;
        }

        id = string.Empty;
        return false;
    }
}
