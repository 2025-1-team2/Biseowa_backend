using Microsoft.SemanticKernel;
using System.Net.Http;
using System.Text.Json;
using SummaryAgent.Models;

namespace SummaryAgent.Services;

public class TranscriptionPlugin
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSummaryService _summaryService = new();
    private readonly SummaryDbService _dbService = new();

    public TranscriptionPlugin(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [KernelFunction]
    public async IAsyncEnumerable<string> TranscribeStreamAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/transcriptionstream");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                TranscriptionItem? item = null;
                try
                {
                    item = JsonSerializer.Deserialize<TranscriptionItem>(line);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON parsing error: {ex.Message}");
                }

                if (item != null)
                {
                    var summary = await _summaryService.SummarizeAsync(item.Text);
                    await _dbService.SaveSummaryAsync(item.User, summary);
                    yield return $"{item.User}: {summary}";
                }

            }
        }
    }
}