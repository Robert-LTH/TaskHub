using System;
using TaskHub.Abstractions;

namespace AppDomainServicePlugin;

public class AppDomainServicePlugin : IServicePlugin
{
    public string Name => "appdomain";

    public object GetService() => AppDomain.CurrentDomain;
}

