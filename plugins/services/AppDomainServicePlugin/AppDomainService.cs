using System;
using TaskHub.Abstractions;

namespace AppDomainServicePlugin;

public class AppDomainServicePlugin : IServicePlugin
{
    public IServiceProvider Services { get; private set; } = default!;

    public string Name => "appdomain";

    public void OnLoaded(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public object GetService() => AppDomain.CurrentDomain;
}


