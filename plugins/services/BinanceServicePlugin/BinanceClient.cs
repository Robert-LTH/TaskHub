using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BinanceServicePlugin;

/// <summary>
/// Simple client for the Binance Spot REST API.
/// OpenAPI spec: https://binance.github.io/binance-api-swagger/spot_api.yaml
/// </summary>
public class BinanceClient
{
    private readonly HttpClient _http;

    public BinanceClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://api.binance.com/api/v3/");
        }
    }

    /// <summary>
    /// Gets current server time.
    /// </summary>
    public async Task<ServerTime?> GetServerTimeAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("time", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ServerTime>(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets basic exchange information.
    /// </summary>
    public async Task<ExchangeInfo?> GetExchangeInfoAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync("exchangeInfo", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ExchangeInfo>(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets current average price for a symbol.
    /// </summary>
    public async Task<AveragePrice?> GetAveragePriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"avgPrice?symbol={Uri.EscapeDataString(symbol)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<AveragePrice>(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets latest price for a symbol.
    /// </summary>
    public async Task<TickerPrice?> GetTickerPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"ticker/price?symbol={Uri.EscapeDataString(symbol)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<TickerPrice>(stream, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Requests a conversion quote.
    /// </summary>
    public Task<JsonElement?> GetConvertQuoteAsync(
        string fromAsset,
        string toAsset,
        string? fromAmount = null,
        string? toAmount = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            ["fromAsset"] = fromAsset,
            ["toAsset"] = toAsset
        };
        if (!string.IsNullOrEmpty(fromAmount)) query["fromAmount"] = fromAmount;
        if (!string.IsNullOrEmpty(toAmount)) query["toAmount"] = toAmount;
        return SendAsync(HttpMethod.Post, "https://api.binance.com/sapi/v1/convert/getQuote", query, null, cancellationToken);
    }

    /// <summary>
    /// Accepts a previously returned quote.
    /// </summary>
    public Task<JsonElement?> AcceptConvertQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            ["quoteId"] = quoteId
        };
        return SendAsync(HttpMethod.Post, "https://api.binance.com/sapi/v1/convert/acceptQuote", query, null, cancellationToken);
    }

    /// <summary>
    /// Retrieves status for a convert order.
    /// </summary>
    public Task<JsonElement?> GetConvertOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            ["orderId"] = orderId
        };
        return SendAsync(HttpMethod.Get, "https://api.binance.com/sapi/v1/convert/orderStatus", query, null, cancellationToken);
    }

    /// <summary>
    /// Lists convert trade history.
    /// </summary>
    public Task<JsonElement?> GetConvertTradeFlowAsync(
        long startTime,
        long endTime,
        int? page = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string>
        {
            ["startTime"] = startTime.ToString(),
            ["endTime"] = endTime.ToString()
        };
        if (page.HasValue) query["page"] = page.Value.ToString();
        if (limit.HasValue) query["limit"] = limit.Value.ToString();
        return SendAsync(HttpMethod.Get, "https://api.binance.com/sapi/v1/convert/tradeFlow", query, null, cancellationToken);
    }

    /// <summary>
    /// Sends a raw request to any Binance endpoint.
    /// </summary>
    public async Task<JsonElement?> SendAsync(
        HttpMethod method,
        string endpoint,
        Dictionary<string, string>? query = null,
        JsonElement? body = null,
        CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder(endpoint);
        if (query != null && query.Count > 0)
        {
            builder.Append('?');
            var first = true;
            foreach (var kvp in query)
            {
                if (!first)
                {
                    builder.Append('&');
                }
                first = false;
                builder.Append(Uri.EscapeDataString(kvp.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(kvp.Value));
            }
        }

        var request = new HttpRequestMessage(method, builder.ToString());
        if (body.HasValue && method != HttpMethod.Get)
        {
            request.Content = new StringContent(body.Value.GetRawText(), Encoding.UTF8, "application/json");
        }

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
    }
}

