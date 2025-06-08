using Microsoft.SemanticKernel;
using SummaryAgent.Services;

var builder = Kernel.CreateBuilder();
var httpClient = new HttpClient();

var transcriptionPlugin = new TranscriptionPlugin(httpClient);
builder.Plugins.AddFromObject(transcriptionPlugin, "OllamaTranscription");

var kernel = builder.Build();

await foreach (var result in kernel.InvokeStreamingAsync("OllamaTranscription", "TranscribeStreamAsync"))
{
    Console.WriteLine($"[Partial Result] {result}");
}
