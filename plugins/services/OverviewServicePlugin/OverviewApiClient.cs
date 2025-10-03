using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OverviewServicePlugin;

public class OverviewApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public OverviewApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://posbeta.eklientref.se/overview/");
        }

        _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private static void AddQueryParam(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }
    }

    private static void AddQueryParam(List<string> query, string name, int? value)
    {
        if (value.HasValue)
        {
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Value.ToString(CultureInfo.InvariantCulture))}");
        }
    }

    private static void AddQueryParam(List<string> query, string name, long? value)
    {
        if (value.HasValue)
        {
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Value.ToString(CultureInfo.InvariantCulture))}");
        }
    }

    private static void AddQueryParam(List<string> query, string name, bool? value)
    {
        if (value.HasValue)
        {
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.Value ? "true" : "false")}");
        }
    }

    private static void AddQueryParam(List<string> query, string name, IEnumerable<string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
            }
        }
    }

    private static void AddQueryParam(List<string> query, string name, IEnumerable<int>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value.ToString(CultureInfo.InvariantCulture))}");
        }
    }

    private static string BuildQueryString(List<string> query)
    {
        return query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
    }

    private async Task<T?> ReadAsAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        if (typeof(T) == typeof(string))
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return (T?)(object?)text;
        }

        if (typeof(T) == typeof(bool))
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (bool.TryParse(text, out var parsed))
            {
                return (T?)(object)parsed;
            }
        }

        if (typeof(T) == typeof(int))
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return (T?)(object)parsed;
            }
        }

        if (typeof(T) == typeof(long))
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return (T?)(object)parsed;
            }
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<T>> ReadListAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var items = await ReadAsAsync<List<T>>(response, cancellationToken).ConfigureAwait(false);
        return items ?? new List<T>();
    }

    private StringContent CreateJsonContent<T>(T value)
    {
        var payload = JsonSerializer.Serialize(value, _serializerOptions);
        return new StringContent(payload, Encoding.UTF8, "application/json");
    }

        public async Task<OverviewVersion?> GetAssemblyVersionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/About/GetAssemblyVersion", cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<OverviewVersion>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> DeleteComponentTemplateAsync(ComponentTemplateDto template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));
        using var content = CreateJsonContent(template);
        using var response = await _httpClient.PutAsync("api/configuration/DeleteCompnentTemplate", content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> UpdateComponentTemplateAsync(ComponentTemplateDto template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));
        using var content = CreateJsonContent(template);
        using var response = await _httpClient.PutAsync("api/configuration/UpdateCompnentTemplate", content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> CreateComponentTemplateAsync(ComponentTemplateDto template, CancellationToken cancellationToken = default)
    {
        if (template is null) throw new ArgumentNullException(nameof(template));
        using var content = CreateJsonContent(template);
        using var response = await _httpClient.PutAsync("api/configuration/CreateComponentTemplate", content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ComponentTemplateDto>> GetComponentTemplatesAsync(int? phaseId = null, int? releaseId = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "phase", phaseId);
        AddQueryParam(query, "release", releaseId);
        var uri = "api/configuration/GetComponentTemplates" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<ComponentTemplateDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task TestSystemPhaseSetAsync(SlimPhaseRunDto payload, CancellationToken cancellationToken = default)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        using var content = CreateJsonContent(payload);
        using var response = await _httpClient.PostAsync("api/configuration/TestSystemPhaseSet", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> TestSystemPhaseGetAsync(int releaseId, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "releaseID", releaseId);
        var uri = "api/configuration/TestSystemPhaseget" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<int>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetSysmanClientsFromCollectionAsync(string? phaseName = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "PhaseName", phaseName);
        var uri = "api/overview/GetSysmanClientsFromCollection" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AreaDto>> GetAreaForPhaseAsync(int phaseId, string? template = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "phaseID", phaseId);
        AddQueryParam(query, "template", template);
        var uri = "api/OverviewAPI/GetAreaForPhase" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<AreaDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveNewAreaAsync(AreaDto area, CancellationToken cancellationToken = default)
    {
        if (area is null) throw new ArgumentNullException(nameof(area));
        using var content = CreateJsonContent(area);
        using var response = await _httpClient.PostAsync("api/OverviewAPI/SaveNewArea", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateComponentAsync(ComponentDto component, CancellationToken cancellationToken = default)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        using var content = CreateJsonContent(component);
        using var response = await _httpClient.PostAsync("api/OverviewAPI/CreateComponent", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateTesterRunAsync(int releaseId, TestersDto tester, CancellationToken cancellationToken = default)
    {
        if (tester is null) throw new ArgumentNullException(nameof(tester));
        var query = new List<string>();
        AddQueryParam(query, "ReleaseID", releaseId);
        var uri = "api/OverviewAPI/CreateTesterRun" + BuildQueryString(query);
        using var content = CreateJsonContent(tester);
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> UpdateAreaAsync(AreaDto area, CancellationToken cancellationToken = default)
    {
        if (area is null) throw new ArgumentNullException(nameof(area));
        using var content = CreateJsonContent(area);
        using var response = await _httpClient.PostAsync("api/OverviewAPI/UpdateArea", content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> DeleteAreaAsync(int areaId, string template, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(template)) throw new ArgumentException("Template must be provided.", nameof(template));
        var query = new List<string>();
        AddQueryParam(query, "AreaID", areaId);
        AddQueryParam(query, "Template", template);
        var uri = "api/OverviewAPI/DeleteArea" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReleaseversionDto>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/OverviewAPI/GetReleases", cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<ReleaseversionDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PlanDto>> GetMatchingPlansAsync(int releaseId, MatchingPlansOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "ReleaseID", releaseId);
        if (options is not null)
        {
            AddQueryParam(query, "Name", options.Name);
            AddQueryParam(query, "Planner", options.Planner);
            AddQueryParam(query, "PlanApprover", options.PlanApprover);
            AddQueryParam(query, "Comment", options.Comment);
            AddQueryParam(query, "PlanStatus", options.PlanStatus);
            AddQueryParam(query, "ScheduledDate", options.ScheduledDate);
            AddQueryParam(query, "ScheduledTo", options.ScheduledTo);
            AddQueryParam(query, "Page", options.Page);
            AddQueryParam(query, "PageSize", options.PageSize);
            AddQueryParam(query, "SortColumn", options.SortColumn);
            AddQueryParam(query, "IsAscending", options.IsAscending);
        }

        var uri = "api/OverviewAPI/GetMatchingPlans" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<PlanDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientDtoDataPage?> GetMatchingClientsPagedImprovedAsync(MatchingClientsQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        var parameters = new List<string>();
        AddQueryParam(parameters, "Name", query.Name);
        AddQueryParam(parameters, "Win10Ready", query.Win10Ready);
        AddQueryParam(parameters, "AllSystemsReleaseReady", query.AllSystemsReleaseReady);
        AddQueryParam(parameters, "Owner", query.Owner);
        AddQueryParam(parameters, "System", query.System);
        AddQueryParam(parameters, "Application", query.Application);
        AddQueryParam(parameters, "Location", query.Location);
        AddQueryParam(parameters, "Department", query.Department);
        AddQueryParam(parameters, "ProcureDateFrom", query.ProcureDateFrom);
        AddQueryParam(parameters, "ProcureDateTo", query.ProcureDateTo);
        AddQueryParam(parameters, "Planned", query.Planned);
        AddQueryParam(parameters, "CostCenter", query.CostCenter);
        AddQueryParam(parameters, "HasGeneralDescription", query.HasGeneralDescription);
        AddQueryParam(parameters, "IsSpecial", query.IsSpecial);
        AddQueryParam(parameters, "Taggar", query.Taggar);
        AddQueryParam(parameters, "Taggarmin", query.TaggarMin);
        AddQueryParam(parameters, "ReleaseID", query.ReleaseId);
        AddQueryParam(parameters, "PlanId", query.PlanId);
        AddQueryParam(parameters, "PageSize", query.PageSize);
        AddQueryParam(parameters, "PageNumber", query.PageNumber);
        AddQueryParam(parameters, "OrderByColumn", query.OrderByColumn);
        AddQueryParam(parameters, "IsAscending", query.IsAscending);
        AddQueryParam(parameters, "Pcnames", query.PcNames);
        AddQueryParam(parameters, "OpSystem", query.OpSystem);

        var uri = "api/OverviewAPI/GetMatchingClientsPagedImproved" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<ClientDtoDataPage>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClientDtoDataPage?> GetMatchingClientsPagedImprovedSynchAsync(MatchingClientsQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        var parameters = new List<string>();
        AddQueryParam(parameters, "Name", query.Name);
        AddQueryParam(parameters, "Win10Ready", query.Win10Ready);
        AddQueryParam(parameters, "AllSystemsReleaseReady", query.AllSystemsReleaseReady);
        AddQueryParam(parameters, "AllSystemsExeptNotOk", query.AllSystemsExceptNotOk);
        AddQueryParam(parameters, "Owner", query.Owner);
        AddQueryParam(parameters, "System", query.System);
        AddQueryParam(parameters, "Application", query.Application);
        AddQueryParam(parameters, "Location", query.Location);
        AddQueryParam(parameters, "Department", query.Department);
        AddQueryParam(parameters, "ProcureDateFrom", query.ProcureDateFrom);
        AddQueryParam(parameters, "ProcureDateTo", query.ProcureDateTo);
        AddQueryParam(parameters, "Planned", query.Planned);
        AddQueryParam(parameters, "CostCenter", query.CostCenter);
        AddQueryParam(parameters, "HasGeneralDescription", query.HasGeneralDescription);
        AddQueryParam(parameters, "IsSpecial", query.IsSpecial);
        AddQueryParam(parameters, "Taggar", query.Taggar);
        AddQueryParam(parameters, "ReleaseID", query.ReleaseId);
        AddQueryParam(parameters, "PlanId", query.PlanId);
        AddQueryParam(parameters, "PageSize", query.PageSize);
        AddQueryParam(parameters, "PageNumber", query.PageNumber);
        AddQueryParam(parameters, "OrderByColumn", query.OrderByColumn);
        AddQueryParam(parameters, "IsAscending", query.IsAscending);
        AddQueryParam(parameters, "Pcnames", query.PcNames);

        var uri = "api/OverviewAPI/GetMatchingClientsPagedImprovedSynch" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<ClientDtoDataPage>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AddClientsToPlanAsync(int planId, IEnumerable<string> clientNames, CancellationToken cancellationToken = default)
    {
        if (clientNames is null) throw new ArgumentNullException(nameof(clientNames));
        var names = clientNames.ToList();
        if (names.Count == 0) throw new ArgumentException("At least one client name must be provided.", nameof(clientNames));

        var parameters = new List<string>();
        AddQueryParam(parameters, "PlanID", planId);
        AddQueryParam(parameters, "ClientNames", names);

        var uri = "api/OverviewAPI/AddClientsToPlan" + BuildQueryString(parameters);
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<bool>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveClientsAsync(IEnumerable<string> clientNames, CancellationToken cancellationToken = default)
    {
        if (clientNames is null) throw new ArgumentNullException(nameof(clientNames));
        var names = clientNames.ToList();
        if (names.Count == 0) throw new ArgumentException("At least one client name must be provided.", nameof(clientNames));

        var parameters = new List<string>();
        AddQueryParam(parameters, "clients", names);

        var uri = "api/OverviewAPI/RemoveClients" + BuildQueryString(parameters);
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<bool>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetPlanStatusAsync(string planName, string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("Plan name must be provided.", nameof(planName));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status must be provided.", nameof(status));

        var parameters = new List<string>();
        AddQueryParam(parameters, "planName", planName);
        AddQueryParam(parameters, "status", status);

        var uri = "api/OverviewAPI/SetPlanStatus" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task SetPlanStatusWithPutAsync(string planName, string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("Plan name must be provided.", nameof(planName));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Status must be provided.", nameof(status));

        var parameters = new List<string>();
        AddQueryParam(parameters, "planName", planName);
        AddQueryParam(parameters, "status", status);

        var uri = "api/OverviewAPI/SetPlanStatus" + BuildQueryString(parameters);
        using var request = new HttpRequestMessage(HttpMethod.Put, uri);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MailFromSyncDto>> GetMailsForSyncAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(releaseId)) throw new ArgumentException("Release identifier must be provided.", nameof(releaseId));

        var parameters = new List<string>();
        AddQueryParam(parameters, "ReleaseID", releaseId);

        var uri = "api/OverviewAPI/GetMailsForSync" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<MailFromSyncDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MailDto?> GetMailConfigAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/OverviewAPI/GetMailConfig", cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<MailDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> CreateNewPlanAsync(NewPlan plan, CancellationToken cancellationToken = default)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        using var content = CreateJsonContent(plan);
        using var response = await _httpClient.PostAsync("api/OverviewAPI/CreateNewPlan", content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<string>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> SetDeploymentStatusAsync(int planId, string clientId, int deploymentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("Client identifier must be provided.", nameof(clientId));

        var parameters = new List<string>();
        AddQueryParam(parameters, "planID", planId);
        AddQueryParam(parameters, "clientID", clientId);
        AddQueryParam(parameters, "deploymentID", deploymentId);

        var uri = "api/OverviewAPI/SetDeploymentStatus" + BuildQueryString(parameters);
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<bool>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OVTags>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/OverviewAPI/GetTags", cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<OVTags>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> GetDoesPlanExistAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Plan name must be provided.", nameof(name));
        var parameters = new List<string>();
        AddQueryParam(parameters, "name", name);

        var uri = "api/OverviewAPI/GetDoesPlanExist" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<bool>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlanDto?> GetPlanByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Plan name must be provided.", nameof(name));
        var parameters = new List<string>();
        AddQueryParam(parameters, "name", name);

        var uri = "api/OverviewAPI/GetPlanByName" + BuildQueryString(parameters);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<PlanDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PlanDto>> GetPlansWithSearchAsync(int releaseId, MatchingPlansOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "ReleaseID", releaseId);
        if (options is not null)
        {
            AddQueryParam(query, "Name", options.Name);
            AddQueryParam(query, "Planner", options.Planner);
            AddQueryParam(query, "PlanApprover", options.PlanApprover);
            AddQueryParam(query, "Comment", options.Comment);
            AddQueryParam(query, "PlanStatus", options.PlanStatus);
            AddQueryParam(query, "ScheduledDate", options.ScheduledDate);
            AddQueryParam(query, "ScheduledTo", options.ScheduledTo);
            AddQueryParam(query, "Page", options.Page);
            AddQueryParam(query, "PageSize", options.PageSize);
            AddQueryParam(query, "SortColumn", options.SortColumn);
            AddQueryParam(query, "IsAscending", options.IsAscending);
        }

        var uri = "api/OverviewAPI/GetPlansWithSearch" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadListAsync<PlanDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PlanDto?> GetPlansClientsAsync(long planId, int releaseId, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "id", planId);
        AddQueryParam(query, "release", releaseId);

        var uri = "api/OverviewAPI/GetPlansClients" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<PlanDto>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DiagClientSysVConpareSysXDataPage?> GetClientsWithFailedSystemsForReleaseAsync(int releaseId, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        AddQueryParam(query, "releaseID", releaseId);

        var uri = "api/ovoneapi/GetClientsWithFailedSystemsForReleaseV3" + BuildQueryString(query);
        using var response = await _httpClient.GetAsync(uri, cancellationToken).ConfigureAwait(false);
        return await ReadAsAsync<DiagClientSysVConpareSysXDataPage>(response, cancellationToken).ConfigureAwait(false);
    }

    public sealed class MatchingPlansOptions
    {
        public string? Name { get; set; }
        public string? Planner { get; set; }
        public string? PlanApprover { get; set; }
        public string? Comment { get; set; }
        public IEnumerable<string>? PlanStatus { get; set; }
        public string? ScheduledDate { get; set; }
        public string? ScheduledTo { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? SortColumn { get; set; }
        public bool? IsAscending { get; set; }
    }

    public sealed class MatchingClientsQuery
    {
        public string? Name { get; set; }
        public bool? Win10Ready { get; set; }
        public bool? AllSystemsReleaseReady { get; set; }
        public bool? AllSystemsExceptNotOk { get; set; }
        public string? Owner { get; set; }
        public string? System { get; set; }
        public string? Application { get; set; }
        public string? Location { get; set; }
        public string? Department { get; set; }
        public string? ProcureDateFrom { get; set; }
        public string? ProcureDateTo { get; set; }
        public bool? Planned { get; set; }
        public string? CostCenter { get; set; }
        public bool? HasGeneralDescription { get; set; }
        public bool? IsSpecial { get; set; }
        public IEnumerable<string>? Taggar { get; set; }
        public IEnumerable<string>? TaggarMin { get; set; }
        public int? ReleaseId { get; set; }
        public int? PlanId { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public string? OrderByColumn { get; set; }
        public bool? IsAscending { get; set; }
        public IEnumerable<string>? PcNames { get; set; }
        public IEnumerable<string>? OpSystem { get; set; }
    }
}
