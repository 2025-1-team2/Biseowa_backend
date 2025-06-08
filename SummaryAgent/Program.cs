using Microsoft.Extensions.Configuration;
using SummaryAgent.Services;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

// 인스턴스 직접 생성
var httpClient = new HttpClient();
var plugin = new TranscriptionPlugin(httpClient, config);

// TranscribeStreamAsync 직접 실행
await foreach (var result in plugin.TranscribeStreamAsync())
{
    Console.WriteLine($"[Direct Result] {result}");
}
