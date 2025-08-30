using System;

namespace TaskHub.Abstractions;

public interface IPluginPrerequisites
{
    bool ShouldLoad(IServiceProvider services, out string? reason);
}
