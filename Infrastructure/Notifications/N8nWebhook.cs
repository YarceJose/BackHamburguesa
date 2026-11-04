using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications;

public sealed class N8nOptions
{
    public const string Section = "N8n";
    public string BaseUrl { get; set; } = "http://localhost:5678";
    /// <summary>Key n8n sends to our integration endpoints (X-Api-Key).</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>n8n "Webhook" node URL that receives order events (WhatsApp flow). Empty = disabled.</summary>
    public string OrdersWebhookUrl { get; set; } = string.Empty;
}

/// <summary>Posts order events to n8n. Fire-and-forget style: failures are logged, never thrown.</summary>
public sealed class N8nWebhook(HttpClient http, IOptions<N8nOptions> options, ILogger<N8nWebhook> logger)
{
    public async Task SendAsync(string eventName, object payload, CancellationToken ct)
    {
        var url = options.Value.OrdersWebhookUrl;
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await http.PostAsJsonAsync(url, new { @event = eventName, sentAt = DateTime.UtcNow, data = payload }, timeout.Token);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("n8n webhook answered {Status} for {Event}", (int)response.StatusCode, eventName);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("n8n webhook unreachable for {Event}: {Message}", eventName, ex.Message);
        }
    }
}
