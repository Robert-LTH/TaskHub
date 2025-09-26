using System.Text.Json;
using System.Text.Json.Serialization;

namespace SysManServicePlugin;

public abstract class ExtensibleObject
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    public void SetProperty<T>(string propertyName, T value)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Property name must be provided.", nameof(propertyName));
        }

        AdditionalProperties ??= new Dictionary<string, JsonElement>();
        AdditionalProperties[propertyName] = JsonSerializer.SerializeToElement(value);
    }

    public bool TryGetProperty(string propertyName, out JsonElement value)
    {
        if (AdditionalProperties is not null && AdditionalProperties.TryGetValue(propertyName, out var element))
        {
            value = element;
            return true;
        }

        value = default;
        return false;
    }
}

public abstract class PagedResultBase<TItem> : ExtensibleObject
{
    [JsonPropertyName("items")]
    public List<TItem> Items { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("skip")]
    public int? Skip { get; set; }

    [JsonPropertyName("take")]
    public int? Take { get; set; }
}

#region Access Resource

public sealed class AccessResourceSearchItemPagedResult : PagedResultBase<AccessResourceSearchItem>
{
}

public sealed class GroupSearchItemPagedResult : PagedResultBase<GroupSearchItem>
{
}

public sealed class AccessResource : ExtensibleObject
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}

public sealed class AccessResourceSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

public sealed class GroupSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public sealed class AccessMemberSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public sealed class MembersMapAccessResourceResult : ExtensibleObject
{
    [JsonPropertyName("mappedMembers")]
    public List<Guid> MappedMembers { get; set; } = new();

    [JsonPropertyName("unmappedMembers")]
    public List<Guid> UnmappedMembers { get; set; } = new();
}

public sealed class ApiErrorMessage : ExtensibleObject
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}

public abstract class AccessResourceCommandBase : ExtensibleObject
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement>? Attributes { get; set; }
}

public sealed class UpsertAccessResourceV2Command : AccessResourceCommandBase
{
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("configurationRootId")]
    public Guid? ConfigurationRootId { get; set; }
}

public sealed class PatchAccessResourceV2Command : AccessResourceCommandBase
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class MapAccessResourceWithMembersV2Command : ExtensibleObject
{
    [JsonPropertyName("membersToMap")]
    public List<Guid> MembersToMap { get; set; } = new();

    [JsonPropertyName("membersToUnmap")]
    public List<Guid> MembersToUnmap { get; set; } = new();
}

#endregion

#region Client

public sealed class Client : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ClientSearchItemPagedResult : PagedResultBase<ClientSearchItem>
{
}

public sealed class ClientSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class PatchClientBatchV2Command : ExtensibleObject
{
    [JsonPropertyName("batch")]
    public List<PatchClientV2Command> Batch { get; set; } = new();
}

public sealed class CreateClientBatchV2Command : ExtensibleObject
{
    [JsonPropertyName("batch")]
    public List<CreateClientV2Command> Batch { get; set; } = new();
}

public sealed class PatchClientV2Command : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class CreateClientV2Command : ExtensibleObject
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class ActivateClientV2Command : ExtensibleObject
{
}

public sealed class DeactivateClientV2Command : ExtensibleObject
{
}

public sealed class DeleteClientV2Command : ExtensibleObject
{
}

public sealed class VerificationResult : ExtensibleObject
{
    [JsonPropertyName("isValid")]
    public bool? IsValid { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class MoveClientsCommand : ExtensibleObject
{
}

public sealed class ImportComputersCommand : ExtensibleObject
{
}

public sealed class ImportCountResult : ExtensibleObject
{
    [JsonPropertyName("imported")]
    public int? Imported { get; set; }

    [JsonPropertyName("failed")]
    public int? Failed { get; set; }
}

public sealed class ImportTargetGroupMembershipsCommand : ExtensibleObject
{
}

public sealed class ImportTargetMembershipResult : ExtensibleObject
{
}

public sealed class ImportComputerDetailsCommand : ExtensibleObject
{
}

public sealed class ImportClientDetailsCountResult : ExtensibleObject
{
}

public sealed class ImportComputerDeploymentsCommand : ExtensibleObject
{
}

public sealed class ImportClientDeploymentCountResult : ExtensibleObject
{
}

public sealed class GetClientsByNamesQuery : ExtensibleObject
{
    [JsonPropertyName("names")]
    public List<string> Names { get; set; } = new();
}

public sealed class Target : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

public sealed class OperatingSystemSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class TargetSearchItemPagedResult : PagedResultBase<TargetSearchItem>
{
}

public sealed class TargetSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

public sealed class StartDeploymentForClientV2Command : ExtensibleObject
{
}

public sealed class ApplyDeploymentTemplateCommand : ExtensibleObject
{
}

public sealed class ResolvedDeploymentTemplate : ExtensibleObject
{
}

public sealed class ApplyDeploymentTemplatePreviewCommand : ExtensibleObject
{
}

public sealed class DeploymentPreview : ExtensibleObject
{
}

public sealed class StartClientExchangeV2Command : ExtensibleObject
{
    [JsonPropertyName("sourceClientId")]
    public long? SourceClientId { get; set; }

    [JsonPropertyName("destinationClientId")]
    public long? DestinationClientId { get; set; }
}

public sealed class ClientExchangeResult : ExtensibleObject
{
}

public sealed class StartClientBatchExchangeV2Command : ExtensibleObject
{
    [JsonPropertyName("batch")]
    public List<StartClientExchangeV2Command> Batch { get; set; } = new();
}

public sealed class ClientInformation : ExtensibleObject
{
}

public sealed class LocalClientInformation : ExtensibleObject
{
}

public sealed class CollectionSearchItemPagedResult : PagedResultBase<CollectionSearchItem>
{
}

public sealed class CollectionSearchItem : ExtensibleObject
{
    [JsonPropertyName("collectionId")]
    public string? CollectionId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("targetType")]
    public string? TargetType { get; set; }
}

public sealed class CollectionMembershipSearchItem : ExtensibleObject
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class MapTargetsToGroupsCommand : ExtensibleObject
{
}

public sealed class GroupMapTargetResult : ExtensibleObject
{
    [JsonPropertyName("mappedTargets")]
    public List<string> MappedTargets { get; set; } = new();

    [JsonPropertyName("unmappedTargets")]
    public List<string> UnmappedTargets { get; set; } = new();
}

public sealed class MapTargetsToCollectionsCommand : ExtensibleObject
{
}

public sealed class CollectionMapTargetResult : ExtensibleObject
{
    [JsonPropertyName("mappedTargets")]
    public List<string> MappedTargets { get; set; } = new();

    [JsonPropertyName("unmappedTargets")]
    public List<string> UnmappedTargets { get; set; } = new();
}

public sealed class FindRecoveryKeyV2Command : ExtensibleObject
{
}

public sealed class FindRecoveryKeyResult : ExtensibleObject
{
    [JsonPropertyName("recoveryKey")]
    public string? RecoveryKey { get; set; }
}

public sealed class ClientNotificationV2Command : ExtensibleObject
{
}

public sealed class ComputerHealth : ExtensibleObject
{
    [JsonPropertyName("targetId")]
    public long? TargetId { get; set; }

    [JsonPropertyName("targetName")]
    public string? TargetName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset? LastUpdated { get; set; }
}

public sealed class DeleteComputerHealthCommand : ExtensibleObject
{
}

public sealed class ComputerHealthPagedResult : PagedResultBase<ComputerHealth>
{
}

public sealed class ClientUploadItem : ExtensibleObject
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public sealed class ClientUploadItemPagedResult : PagedResultBase<ClientUploadItem>
{
}

public sealed class ExchangeUploadItem : ExtensibleObject
{
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public sealed class ExchangeUploadItemPagedResult : PagedResultBase<ExchangeUploadItem>
{
}

#endregion
