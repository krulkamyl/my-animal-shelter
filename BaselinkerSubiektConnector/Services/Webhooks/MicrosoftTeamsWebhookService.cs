using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class TeamsWebhookClient
{
    private readonly string _webhookUrl;
    private readonly HttpClient _httpClient;

    public TeamsWebhookClient(string webhookUrl)
    {
        _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
        _httpClient = new HttpClient();
    }

    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Wiadomość nie może być pusta.", nameof(message));
        }

        var payload = new
        {
            text = message
        };

        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_webhookUrl, content);

        response.EnsureSuccessStatusCode();
    }
}
