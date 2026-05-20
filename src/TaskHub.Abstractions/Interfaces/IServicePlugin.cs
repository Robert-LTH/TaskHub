using System;

namespace TaskHub.Abstractions;

public interface IServicePlugin
{
    string Name { get; }
    IServiceProvider Services { get; }
    void OnLoaded(IServiceProvider services);
    object GetService();
}

public interface IServicePlugin<out TService> : IServicePlugin
{
    new TService GetService();
}
