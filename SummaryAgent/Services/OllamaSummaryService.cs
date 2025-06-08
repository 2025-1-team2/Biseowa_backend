using System.Net.Http;
using System.Net.Http.Json;

namespace SummaryAgent.Services;

public class OllamaSummaryService
{
    private readonly HttpClient _client = new();

    public async Task<string> SummarizeAsync(string input)
    {
        var response = await _client.PostAsJsonAsync("http://localhost:11434/api/generate", new
        {
            model = "mistral",
            prompt = $"다음 문장을 요약해줘: {input}"
        });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return result?["response"]?.ToString() ?? "요약 실패";
    }
}