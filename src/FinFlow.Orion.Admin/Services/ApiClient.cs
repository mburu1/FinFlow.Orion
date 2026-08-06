using System.Net.Http.Json;
using FinFlow.Orion.Contracts.Common;

namespace FinFlow.Orion.Admin.Services;

public sealed class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[ApiClient] GET {Endpoint}", endpoint);
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiClient] GET failed — Endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? body = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[ApiClient] POST {Endpoint}", endpoint);
            var response = await _httpClient.PostAsJsonAsync(endpoint, body, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiClient] POST failed — Endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<T?> PatchAsync<T>(string endpoint, object? body = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[ApiClient] PATCH {Endpoint}", endpoint);
            var response = await _httpClient.PatchAsJsonAsync(endpoint, body, cancellationToken);
            return await HandleResponse<T>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiClient] PATCH failed — Endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("[ApiClient] DELETE {Endpoint}", endpoint);
            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ApiClient] DELETE failed — Endpoint: {Endpoint}", endpoint);
            throw;
        }
    }

    private async Task<T?> HandleResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "[ApiClient] Error response — Status: {Status} | Content: {Content}",
            response.StatusCode, errorContent);

        throw new HttpRequestException(
            $"API request failed with status {response.StatusCode}: {errorContent}");
    }
}