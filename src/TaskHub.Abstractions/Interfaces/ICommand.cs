namespace TaskHub.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

public interface ICommand
{
    Task<JsonElement> ExecuteAsync(IServicePlugin service, CancellationToken cancellationToken);
}

