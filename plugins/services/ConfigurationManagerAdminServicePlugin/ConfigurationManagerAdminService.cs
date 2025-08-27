using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskHub.Abstractions;

namespace ConfigurationManagerAdminServicePlugin;

public class ConfigurationManagerAdminServicePlugin : IServicePlugin
{
    public string Name => "configurationmanageradmin";

    public object GetService() => new ConfigurationManagerAdminService();

    private class ConfigurationManagerAdminService
    {
        private readonly HttpClient _client;

        public ConfigurationManagerAdminService()
        {
            _client = new HttpClient(new HttpClientHandler
            {
                UseDefaultCredentials = true
            });
        }

        public async Task<OperationResult> Get(string baseUrl, string resource, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{baseUrl.TrimEnd('/')}/{resource.TrimStart('/')}";
                var response = await _client.GetAsync(url, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var element = JsonSerializer.Deserialize<JsonElement>(json);
                return new OperationResult(element, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to query admin service '{resource}': {ex.Message}");
            }
        }

        public async Task<OperationResult> AddDeviceToCollection(string baseUrl, string collectionId, string[] deviceIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{baseUrl.TrimEnd('/')}/collections/{collectionId}/members/devices";
                var json = JsonSerializer.Serialize(deviceIds);
                await _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to add devices to collection '{collectionId}': {ex.Message}");
            }
        }

        public async Task<OperationResult> AddUserToCollection(string baseUrl, string collectionId, string[] userIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{baseUrl.TrimEnd('/')}/collections/{collectionId}/members/users";
                var json = JsonSerializer.Serialize(userIds);
                await _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), cancellationToken);
                return new OperationResult(null, "success");
            }
            catch (Exception ex)
            {
                return new OperationResult(null, $"Failed to add users to collection '{collectionId}': {ex.Message}");
            }
        }
    }
}
