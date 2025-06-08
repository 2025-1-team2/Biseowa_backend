using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace SummaryAgent.Services;

public class OllamaSummaryService
{
    private readonly HttpClient _httpClient = new();

    public async Task<string> SummarizeAsync(string input)
    {
        var requestData = new
        {
            model = "transcriptionstream/transcriptionstream",
            prompt = $"다음 문장을 요약해줘: {input}",
            stream = true
        };

        var requestJson = JsonSerializer.Serialize(requestData);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("[Ollama] 오류 응답: " + response.StatusCode);
            return "요약 실패";
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var fullResponse = new StringBuilder();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (root.TryGetProperty("response", out var responseText))
                {
                    var chunk = responseText.GetString();
                    fullResponse.Append(chunk);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine("[Ollama JSON 파싱 오류]: " + ex.Message);
            }
        }

        var finalSummary = fullResponse.ToString().Trim();
        Console.WriteLine("[Ollama 요약 결과] " + finalSummary);

        return string.IsNullOrWhiteSpace(finalSummary) ? "요약 실패" : finalSummary;
    }
}
