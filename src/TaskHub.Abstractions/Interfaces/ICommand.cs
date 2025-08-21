namespace TaskHub.Abstractions;

using System.Threading;
using System.Threading.Tasks;

public interface ICommand
{
    Task ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken);
}

