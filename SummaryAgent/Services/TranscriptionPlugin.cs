using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;

namespace SummaryAgent.Services;

public class TranscriptionPlugin
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSummaryService _summaryService;
    private readonly SummaryDbService _dbService;

    public TranscriptionPlugin(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _summaryService = new OllamaSummaryService();
        _dbService = new SummaryDbService(config);
    }

    [KernelFunction("TranscribeStream")]
    public async IAsyncEnumerable<string> TranscribeStreamAsync()
    {
        await Task.Yield(); // 비동기 컨텍스트 진입

        // 테스트용 입력 문장
        var user = "지현";
        var inputText = "오늘 회의에서는 프로젝트 목표를 공유했어요.";

        // ✅ Ollama 요약 요청
        var summary = await _summaryService.SummarizeAsync(inputText);
        Console.WriteLine($"[Ollama 요약 결과] {summary}");

        // DB 저장
        await _dbService.SaveSummaryAsync(user, summary);
        Console.WriteLine("[DB 저장 완료]");

        // 콘솔 및 Semantic Kernel 반환
        yield return $"{user}: {summary}";
    }
}
