namespace TaskHub.Abstractions;

public interface IServicePlugin
{
    string Name { get; }
    object GetService();
}
