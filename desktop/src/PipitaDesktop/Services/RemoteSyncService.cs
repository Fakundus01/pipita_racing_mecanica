using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PipitaDesktop.Services;

public sealed class RemoteSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45),
    };

    public async Task TestConnectionAsync(string apiBaseUrl, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(BuildUri(apiBaseUrl, "/"), cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<RemoteAuthResult> RegisterAsync(string apiBaseUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        return await SendAuthRequestAsync(apiBaseUrl, "/api/auth/register", email, password, cancellationToken);
    }

    public async Task<RemoteAuthResult> LoginAsync(string apiBaseUrl, string email, string password, CancellationToken cancellationToken = default)
    {
        return await SendAuthRequestAsync(apiBaseUrl, "/api/auth/login", email, password, cancellationToken);
    }

    public async Task<IReadOnlyList<RemoteProfileSummary>> GetProfilesAsync(string apiBaseUrl, string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, BuildUri(apiBaseUrl, "/api/profiles"), accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<List<RemoteProfileSummary>>(response, cancellationToken) ?? new List<RemoteProfileSummary>();
    }

    public async Task<RemoteProfileSummary> CreateProfileAsync(string apiBaseUrl, string accessToken, string name, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Post, BuildUri(apiBaseUrl, "/api/profiles"), accessToken);
        request.Content = JsonContent.Create(new { name });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<RemoteProfileSummary>(response, cancellationToken)
            ?? throw new InvalidOperationException("La API remota no devolvio el perfil creado.");
    }

    public async Task<RemoteProfileSummary> RenameProfileAsync(string apiBaseUrl, string accessToken, string remoteProfileId, string name, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Put, BuildUri(apiBaseUrl, $"/api/profiles/{remoteProfileId}"), accessToken);
        request.Content = JsonContent.Create(new { name });
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<RemoteProfileSummary>(response, cancellationToken)
            ?? throw new InvalidOperationException("La API remota no devolvio el perfil actualizado.");
    }

    public async Task DeleteProfileAsync(string apiBaseUrl, string accessToken, string remoteProfileId, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Delete, BuildUri(apiBaseUrl, $"/api/profiles/{remoteProfileId}"), accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
    }

    public async Task<RemoteSnapshotMetadata> GetSnapshotMetadataAsync(string apiBaseUrl, string accessToken, string remoteProfileId, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, BuildUri(apiBaseUrl, $"/api/profiles/{remoteProfileId}/snapshot/meta"), accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<RemoteSnapshotMetadata>(response, cancellationToken)
            ?? throw new InvalidOperationException("La API remota no devolvio metadata del snapshot.");
    }

    public async Task<RemoteSnapshotMetadata> UploadSnapshotAsync(string apiBaseUrl, string accessToken, string remoteProfileId, string zipPath, string revision, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(zipPath))
        {
            throw new FileNotFoundException("No existe el snapshot local para subir.", zipPath);
        }

        using var request = CreateAuthorizedRequest(HttpMethod.Post, BuildUri(apiBaseUrl, $"/api/profiles/{remoteProfileId}/snapshot/upload"), accessToken);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(File.OpenRead(zipPath));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "file", Path.GetFileName(zipPath));
        content.Add(new StringContent(revision), "revision");
        request.Content = content;

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<RemoteSnapshotMetadata>(response, cancellationToken)
            ?? throw new InvalidOperationException("La API remota no devolvio el estado del snapshot cargado.");
    }

    public async Task DownloadSnapshotAsync(string apiBaseUrl, string accessToken, string remoteProfileId, string destinationZipPath, CancellationToken cancellationToken = default)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, BuildUri(apiBaseUrl, $"/api/profiles/{remoteProfileId}/snapshot/download"), accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationZipPath)!);
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(destinationZipPath);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private async Task<RemoteAuthResult> SendAuthRequestAsync(string apiBaseUrl, string route, string email, string password, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(BuildUri(apiBaseUrl, route), new { email, password }, cancellationToken);
        await EnsureSuccessAsync(response);
        return await ReadJsonAsync<RemoteAuthResult>(response, cancellationToken)
            ?? throw new InvalidOperationException("La API remota no devolvio credenciales validas.");
    }

    private static string BuildUri(string apiBaseUrl, string route)
    {
        var normalizedBase = apiBaseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalizedBase))
        {
            throw new InvalidOperationException("La URL base de la API es obligatoria.");
        }

        return normalizedBase + route;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string uri, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("No hay una sesion remota activa.");
        }

        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await TryExtractErrorMessageAsync(response);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? "La sesion remota no es valida o vencio."
                : message);
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
            ? $"La operacion remota fallo con estado {(int)response.StatusCode}."
            : message);
    }

    private static async Task<string?> TryExtractErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (payload.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
        }
        catch
        {
            // No hacemos nada: si la respuesta no es JSON devolvemos el texto crudo abajo.
        }

        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

public sealed record RemoteAuthResult(string AccessToken, DateTime ExpiresAtUtc, string Email, string UserId);
public sealed record RemoteProfileSummary(string Id, string Name, string Slug, string? SnapshotRevision, DateTime? SnapshotUploadedAtUtc, long? SnapshotSizeBytes)
{
    public override string ToString() => string.IsNullOrWhiteSpace(SnapshotRevision)
        ? $"{Name} ({Slug})"
        : $"{Name} ({Slug}) | rev {SnapshotRevision}";
}

public sealed record RemoteSnapshotMetadata(bool HasSnapshot, string? Revision, DateTime? UploadedAtUtc, long? SizeBytes, string? FileName);
