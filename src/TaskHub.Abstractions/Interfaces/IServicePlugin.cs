using System.Threading;
using System.Threading.Tasks;

namespace TaskHub.Abstractions;

public interface IServicePlugin
{
    string Name { get; }
    Task<string> GetAsync(string resource, CancellationToken cancellationToken);
}
