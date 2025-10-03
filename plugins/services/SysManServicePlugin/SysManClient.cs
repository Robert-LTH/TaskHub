using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SysManServicePlugin;

public sealed class SysManClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _serializerOptions;

    public SysManClient(HttpClient http, SysManClientOptions options)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));
        
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = options.BaseAddress ?? new Uri("https://localhost/");
        }

        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    #region Access resources

    /// <summary>
    /// Searches the available access resources.
    /// </summary>
    public async Task<AccessResourceSearchItemPagedResult?> SearchAccessResourcesAsync(string? filter = null, int? take = null, int? skip = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/accessresource/search", ("filter", filter), ("take", take), ("skip", skip));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<AccessResourceSearchItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Searches all available groups (not limited to any configuration root).
    /// </summary>
    public async Task<GroupSearchItemPagedResult?> SearchAccessResourceGroupsAsync(string? filter = null, int? take = null, int? skip = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/accessresource/group/search", ("filter", filter), ("take", take), ("skip", skip));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<GroupSearchItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a specific access resource based on the id.
    /// </summary>
    public async Task<AccessResource?> GetAccessResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/v2/accessresource/{id}", cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<AccessResource>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Partially updates an existing access resource.
    /// </summary>
    public async Task<AccessResource?> PatchAccessResourceAsync(Guid id, PatchAccessResourceV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/v2/accessresource/{id}")
        {
            Content = JsonContent.Create(command, options: _serializerOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<AccessResource>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes an access resource.
    /// </summary>
    public async Task<bool> DeleteAccessResourceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/v2/accessresource/{id}", cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Creates or updates an access resource.
    /// </summary>
    public async Task<AccessResource?> CreateAccessResourceAsync(UpsertAccessResourceV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/accessresource", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<AccessResource>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Maps or unmaps members to the provided access resource.
    /// </summary>
    public async Task<MembersMapAccessResourceResult?> UpdateAccessResourceMembersAsync(Guid id, MapAccessResourceWithMembersV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync($"api/v2/accessresource/{id}/member", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<MembersMapAccessResourceResult>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all the members for a given access resource.
    /// </summary>
    public async Task<IReadOnlyList<AccessMemberSearchItem>> GetAccessResourceMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/v2/accessresource/{id}/member", cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<AccessMemberSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<AccessMemberSearchItem>();
    }

    #endregion

    #region Client endpoints

    public async Task<string?> GetClientPasswordAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        using var response = await _http.GetAsync($"api/v2/client/{Uri.EscapeDataString(name)}/password", cancellationToken).ConfigureAwait(false);
        return await ReadStringContentAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Client>> PatchClientsBatchAsync(PatchClientBatchV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var request = new HttpRequestMessage(HttpMethod.Patch, "api/v2/client/batch")
        {
            Content = JsonContent.Create(command, options: _serializerOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var clients = await ReadContentAsync<List<Client>>(response, cancellationToken).ConfigureAwait(false);
        return clients ?? new List<Client>();
    }

    public async Task<IReadOnlyList<Client>> CreateClientsBatchAsync(CreateClientBatchV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/batch", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        var clients = await ReadContentAsync<List<Client>>(response, cancellationToken).ConfigureAwait(false);
        return clients ?? new List<Client>();
    }

    public async Task<Client?> PatchClientAsync(long id, PatchClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/v2/client/{id}")
        {
            Content = JsonContent.Create(command, options: _serializerOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<Client>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Client?> GetClientAsync(long id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/v2/client/{id}", cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<Client>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ActivateClientsAsync(ActivateClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/activate", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateClientsAsync(DeactivateClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/deactivate", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<VerificationResult?> ValidateClientAsync(CreateClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/verify", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VerificationResult?> ValidateClientBatchAsync(IEnumerable<CreateClientV2Command> commands, CancellationToken cancellationToken = default)
    {
        if (commands is null) throw new ArgumentNullException(nameof(commands));

        using var response = await _http.PostAsJsonAsync("api/v2/client/batchVerify", commands, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Client?> CreateClientAsync(CreateClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<Client>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteClientsAsync(DeleteClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var request = new HttpRequestMessage(HttpMethod.Delete, "api/v2/client")
        {
            Content = JsonContent.Create(command, options: _serializerOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MoveClientsToStorageAsync(MoveClientsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/moveToStorage", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MoveClientsToStageAsync(MoveClientsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/moveToStage", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<ImportCountResult?> ImportComputersAsync(ImportComputersCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/importComputers", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ImportCountResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImportTargetMembershipResult?> ImportTargetGroupMembershipsAsync(ImportTargetGroupMembershipsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/importTargetMemberships", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ImportTargetMembershipResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImportClientDetailsCountResult?> ImportComputerDetailsAsync(ImportComputerDetailsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/importComputerDetails", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ImportClientDetailsCountResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImportClientDeploymentCountResult?> ImportComputerDeploymentsAsync(ImportComputerDeploymentsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/importComputerDeployments", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ImportClientDeploymentCountResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Target>> GetClientsByNamesAsync(GetClientsByNamesQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        using var response = await _http.PostAsJsonAsync("api/v2/client/getByNames", query, _serializerOptions, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<Target>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<Target>();
    }

    public async Task<IReadOnlyList<OperatingSystemSearchItem>> GetClientOperatingSystemsAsync(IEnumerable<long> clientIds, CancellationToken cancellationToken = default)
    {
        if (clientIds is null) throw new ArgumentNullException(nameof(clientIds));

        using var response = await _http.PostAsJsonAsync("api/v2/client/getOperatingSystems", clientIds, _serializerOptions, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<OperatingSystemSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<OperatingSystemSearchItem>();
    }

    public async Task<ClientSearchItemPagedResult?> SearchClientsAsync(
        string? filter = null,
        int? take = null,
        int? skip = null,
        string? type = null,
        string? targetActive = null,
        string? referenceFilter = null,
        IEnumerable<long>? favorites = null,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(
            "api/v2/client/search",
            ("filter", filter),
            ("take", take),
            ("skip", skip),
            ("type", type),
            ("targetActive", targetActive),
            ("referenceFilter", referenceFilter),
            ("favorites", favorites));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientSearchItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync(IEnumerable<long>? ids = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/client/list", ("ids", ids ?? Array.Empty<long>()));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<Client>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<Client>();
    }

    public async Task<Client?> FindClientAsync(long? id = null, string? name = null, string? assetTag = null, string? serial = null, string? uuid = null, string? mac = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(
            "api/v2/client/find",
            ("id", id),
            ("name", name),
            ("assetTag", assetTag),
            ("serial", serial),
            ("uuid", uuid),
            ("mac", mac));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<Client>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FindRecoveryKeyResult?> FindClientRecoveryKeyAsync(string name, FindRecoveryKeyV2Command command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync($"api/v2/client/{Uri.EscapeDataString(name)}/recoveryKey", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<FindRecoveryKeyResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TargetSearchItemPagedResult?> GetIncompleteClientsAsync(int? take = null, int? skip = null, string? activeSearchFilter = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/client/GetIncompleteClients", ("take", take), ("skip", skip), ("activeSearchFilter", activeSearchFilter));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<TargetSearchItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Client?> StartClientDeploymentAsync(long id, StartDeploymentForClientV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync($"api/v2/client/{id}/deployment", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<Client>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ResolvedDeploymentTemplate?> ApplyDeploymentTemplateAsync(ApplyDeploymentTemplateCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/applyDeploymentTemplate", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ResolvedDeploymentTemplate>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DeploymentPreview?> PreviewDeploymentTemplateAsync(ApplyDeploymentTemplatePreviewCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/applyDeploymentTemplate/preview", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<DeploymentPreview>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientExchangeResult?> StartClientExchangeAsync(long id, StartClientExchangeV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync($"api/v2/client/{id}/exchange", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientExchangeResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientExchangeResult?> StartClientExchangeBatchAsync(StartClientBatchExchangeV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/exchange/batch", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientExchangeResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientInformation?> GetClientWmiInformationAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        using var response = await _http.GetAsync($"api/v2/client/{Uri.EscapeDataString(name)}/wmiInformation", cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientInformation>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LocalClientInformation?> GetLocalClientInformationAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        using var response = await _http.GetAsync($"api/v2/client/{Uri.EscapeDataString(name)}/localInformation", cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<LocalClientInformation>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientInformation?> GetClientSccmInformationAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        using var response = await _http.GetAsync($"api/v2/client/{Uri.EscapeDataString(name)}/sccmInformation", cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientInformation>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GroupSearchItem>> GetClientGroupsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        using var response = await _http.GetAsync($"api/v2/client/{Uri.EscapeDataString(name)}/group", cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<GroupSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<GroupSearchItem>();
    }

    public async Task<IReadOnlyList<GroupSearchItem>> GetGroupsByTargetsAsync(IEnumerable<string> targetNames, CancellationToken cancellationToken = default)
    {
        if (targetNames is null) throw new ArgumentNullException(nameof(targetNames));

        using var response = await _http.PostAsJsonAsync("api/v2/client/getGroups", targetNames, _serializerOptions, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<GroupSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<GroupSearchItem>();
    }

    public async Task<IReadOnlyList<CollectionMembershipSearchItem>> GetClientCollectionsAsync(string name, bool? queryMembershipFilter = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        var uri = BuildUri($"api/v2/client/{Uri.EscapeDataString(name)}/collection", ("queryMembershipFilter", queryMembershipFilter));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<CollectionMembershipSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<CollectionMembershipSearchItem>();
    }

    public async Task<IReadOnlyList<CollectionMembershipSearchItem>> GetCollectionsByTargetsAsync(IEnumerable<string> targetNames, bool? queryMembershipFilter = null, CancellationToken cancellationToken = default)
    {
        if (targetNames is null) throw new ArgumentNullException(nameof(targetNames));

        var uri = BuildUri("api/v2/client/getCollections", ("queryMembershipFilter", queryMembershipFilter));
        using var response = await _http.PostAsJsonAsync(uri, targetNames, _serializerOptions, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<CollectionMembershipSearchItem>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<CollectionMembershipSearchItem>();
    }

    public async Task<GroupMapTargetResult?> UpdateClientGroupsAsync(MapTargetsToGroupsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/group", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<GroupMapTargetResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CollectionMapTargetResult?> UpdateClientCollectionsAsync(MapTargetsToCollectionsCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/collection", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<CollectionMapTargetResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ComputerHealth?> PutClientHealthAsync(ComputerHealth health, CancellationToken cancellationToken = default)
    {
        if (health is null) throw new ArgumentNullException(nameof(health));

        using var response = await _http.PutAsJsonAsync("api/v2/client/health", health, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ComputerHealth>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ComputerHealth?> CreateClientHealthAsync(ComputerHealth health, CancellationToken cancellationToken = default)
    {
        if (health is null) throw new ArgumentNullException(nameof(health));

        using var response = await _http.PostAsJsonAsync("api/v2/client/health", health, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ComputerHealth>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ComputerHealth>> GetClientHealthAsync(long? targetId = null, string? targetName = null, bool? onlyLatest = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/client/health", ("targetId", targetId), ("targetName", targetName), ("onlyLatest", onlyLatest));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<ComputerHealth>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<ComputerHealth>();
    }

    public async Task<bool> DeleteClientHealthAsync(DeleteComputerHealthCommand command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var request = new HttpRequestMessage(HttpMethod.Delete, "api/v2/client/health")
        {
            Content = JsonContent.Create(command, options: _serializerOptions)
        };

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<ComputerHealthPagedResult?> SearchClientHealthAsync(int? take = null, int? skip = null, string? errorTypeFilter = null, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri("api/v2/client/health/search", ("take", take), ("skip", skip), ("errorTypeFilter", errorTypeFilter));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ComputerHealthPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VerificationResult?> ValidateClientNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Client name must be provided.", nameof(name));

        var uri = BuildUri("api/v2/client/validateName", ("name", name));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VerificationResult?> ValidateClientMacAsync(string mac, string? computerName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mac)) throw new ArgumentException("MAC address must be provided.", nameof(mac));

        var uri = BuildUri("api/v2/client/validateMac", ("mac", mac), ("computerName", computerName));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VerificationResult?> ValidateClientUuidAsync(string uuid, string? computerName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uuid)) throw new ArgumentException("UUID must be provided.", nameof(uuid));

        var uri = BuildUri("api/v2/client/validateUuid", ("uuid", uuid), ("computerName", computerName));
        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetClientNotificationActionsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/v2/client/clientNotification/actions", cancellationToken).ConfigureAwait(false);
        var actions = await ReadContentAsync<List<string>>(response, cancellationToken).ConfigureAwait(false) ?? new List<string>();
        return actions;
    }

    public async Task<bool> PostClientNotificationAsync(ClientNotificationV2Command command, CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        using var response = await _http.PostAsJsonAsync("api/v2/client/clientNotification", command, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<VerificationResult?> ValidateClientUploadAsync(ClientUploadItem uploadItem, CancellationToken cancellationToken = default)
    {
        if (uploadItem is null) throw new ArgumentNullException(nameof(uploadItem));

        using var response = await _http.PostAsJsonAsync("api/v2/client/validateUpload", uploadItem, _serializerOptions, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientUploadItemPagedResult?> UploadClientBatchAsync(Stream fileStream, string fileName, string? contentType = null, CancellationToken cancellationToken = default)
    {
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name must be provided.", nameof(fileName));

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        content.Add(streamContent, "batchUploadFile", fileName);

        using var response = await _http.PostAsync("api/v2/client/upload", content, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ClientUploadItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> CreateClientBatchFileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/v2/client/createClientBatchFile", cancellationToken).ConfigureAwait(false);
        return await ReadBinaryContentAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]?> CreateExchangeBatchFileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync("api/v2/client/CreateExchangeBatchFile", cancellationToken).ConfigureAwait(false);
        return await ReadBinaryContentAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExchangeUploadItemPagedResult?> UploadExchangeBatchFileAsync(Stream fileStream, string fileName, string? contentType = null, CancellationToken cancellationToken = default)
    {
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name must be provided.", nameof(fileName));

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType ?? "application/octet-stream");
        content.Add(streamContent, "batchUploadFile", fileName);

        using var response = await _http.PostAsync("api/v2/client/uploadExchangeBatchFile", content, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<ExchangeUploadItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Collection endpoints

    public async Task<CollectionSearchItemPagedResult?> SearchCollectionsAsync(
        string? filter = null,
        int? take = null,
        int? skip = null,
        string? targetType = null,
        bool? builtInFilter = null,
        bool? ignoreSearchPath = null,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(
            "api/v2/collection/search",
            ("filter", filter),
            ("take", take),
            ("skip", skip),
            ("type", targetType),
            ("builtInFilter", builtInFilter),
            ("ignoreSearchPath", ignoreSearchPath));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<CollectionSearchItemPagedResult>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CollectionSearchItem?> FindCollectionAsync(
        string collectionId,
        string? targetType = null,
        bool? ignoreSearchPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentException("Collection id must be provided.", nameof(collectionId));

        var uri = BuildUri(
            "api/v2/collection/find",
            ("collectionId", collectionId),
            ("targetType", targetType),
            ("ignoreSearchPath", ignoreSearchPath));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<CollectionSearchItem>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TargetSearchItem>> ResolveCollectionAsync(
        string collectionId,
        string? targetType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentException("Collection id must be provided.", nameof(collectionId));

        var uri = BuildUri(
            $"api/v2/collection/{Uri.EscapeDataString(collectionId)}/resolve",
            ("targetType", targetType));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        var items = await ReadContentAsync<List<TargetSearchItem>>(response, cancellationToken).ConfigureAwait(false) ?? new List<TargetSearchItem>();
        return items;
    }

    public async Task<VerificationResult?> ValidateCollectionExistsAsync(
        string collectionId,
        string? targetType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(collectionId)) throw new ArgumentException("Collection id must be provided.", nameof(collectionId));

        var uri = BuildUri(
            "api/v2/collection/validateCollectionExists",
            ("collectionId", collectionId),
            ("targetType", targetType));

        using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadContentAsync<VerificationResult>(response, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    private async Task<T?> ReadContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response is null) throw new ArgumentNullException(nameof(response));

        if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
        {
            return default;
        }

        if (response.Content is null)
        {
            return default;
        }

        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return default;
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            return default;
        }

        return await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]?> ReadBinaryContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response is null) throw new ArgumentNullException(nameof(response));

        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            return Array.Empty<byte>();
        }

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        return memory.ToArray();
    }

    private static async Task<string?> ReadStringContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response is null) throw new ArgumentNullException(nameof(response));

        if (!response.IsSuccessStatusCode || response.Content is null)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return null;
        }

        if (stream.CanSeek && stream.Length == 0)
        {
            return null;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: false);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    private static string BuildUri(string path, params (string Name, object? Value)[] parameters)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path must be provided.", nameof(path));

        if (parameters.Length == 0)
        {
            return path;
        }

        var builder = new StringBuilder(path);
        var hasQuery = false;

        foreach (var (name, value) in parameters)
        {
            if (value is null)
            {
                continue;
            }

            if (value is string stringValue)
            {
                AppendParameter(stringValue);
                continue;
            }

            if (value is bool boolValue)
            {
                AppendParameter(boolValue ? "true" : "false");
                continue;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    var formattedItem = FormatValue(item);
                    if (formattedItem is null)
                    {
                        continue;
                    }

                    AppendParameter(formattedItem);
                }

                continue;
            }

            var formatted = FormatValue(value);
            if (formatted is null)
            {
                continue;
            }

            AppendParameter(formatted);

            void AppendParameter(string candidate)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    return;
                }

                builder.Append(hasQuery ? '&' : '?');
                builder.Append(Uri.EscapeDataString(name));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(candidate));
                hasQuery = true;
            }

            static string? FormatValue(object item)
            {
                return item switch
                {
                    string s when string.IsNullOrWhiteSpace(s) => null,
                    bool b => b ? "true" : "false",
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => item.ToString()
                };
            }
        }

        return builder.ToString();
    }
}
