using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PodcastDigest.Services
{
    public class SummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public SummaryService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _apiKey = configuration["Cohere:ApiKey"] ?? throw new Exception("Cohere API key not configured");
        }

        public async Task<string> SummarizeAsync(string transcript)
        {
            var payload = new
            {
                model = "command-light",
                length = "medium",
                format = "paragraph",
                extractiveness = "auto",
                temperature = 0.3,
                text = transcript
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.ai/v1/summarize");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Add("Cohere-Version", "2022-12-06");

            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseString);

            return doc.RootElement.GetProperty("summary").GetString() ?? "No summary returned.";
        }
    }
}
