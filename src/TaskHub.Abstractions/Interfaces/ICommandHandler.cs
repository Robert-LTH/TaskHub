using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Abstractions;

public interface ICommandHandler
{
    string Name { get; }
    Task ExecuteAsync(string arguments, IServicePlugin service, CancellationToken cancellationToken);
}
