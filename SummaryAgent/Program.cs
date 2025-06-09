// program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using SummaryAgent.Services;
using System.Text.Json;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

var httpClient = new HttpClient();
var plugin = new TranscriptionPlugin(httpClient, config);

// ▶️ Minimal API 서버를 백그라운드 Task로 실행
var serverTask = Task.Run(() =>
{
    var builder = WebApplication.CreateBuilder();
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
    });

    var app = builder.Build();
    app.UseCors();

    // GET /context - 최근 요약본 조회
    app.MapGet("/context", async () =>
    {
        string connStr = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
        string resultText = "";

        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = new MySqlCommand("SELECT summary FROM summaries ORDER BY created_at DESC LIMIT 1", conn);
        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            resultText = reader.GetString(0);
        }

        return Results.Json(new { summary = resultText });
    });

    // POST /context - STT 텍스트 요약 후 저장
    app.MapPost("/context", async (HttpRequest req) =>
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

        var sttText = payload?["text"];
        if (string.IsNullOrWhiteSpace(sttText)) return Results.BadRequest("No text provided.");

        // 요약 실행 (간단히 echo 처리로 대체)
        var summary = "[Summary] " + sttText;

        string connStr = config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string not found.");
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        var insertCmd = new MySqlCommand("INSERT INTO summaries (summary) VALUES (@summary)", conn);
        insertCmd.Parameters.AddWithValue("@summary", summary);
        await insertCmd.ExecuteNonQueryAsync();

        return Results.Ok(new { summary });
    });

    // POST /finalize - 회의 종료 시 주최자 에이전트에게 전달 (단순 출력 처리)
    app.MapPost("/finalize", async () =>
    {
        string connStr = "Server=localhost;Database=meetingdb;User ID=root;Password=yourpassword;";
        await using var conn = new MySqlConnection(connStr);
        await conn.OpenAsync();

        var cmd = new MySqlCommand("SELECT summary FROM summaries ORDER BY created_at", conn);
        var reader = await cmd.ExecuteReaderAsync();
        List<string> summaries = new();
        while (await reader.ReadAsync())
        {
            summaries.Add(reader.GetString(0));
        }

        var finalSummary = string.Join("\n", summaries);
        Console.WriteLine("[Final Summary Sent to Host Agent]");
        Console.WriteLine(finalSummary);

        return Results.Ok(new { finalSummary });
    });

    app.Run("http://localhost:5000");
});

// ▶️ 동시에 transcription stream 실행
await foreach (var result in plugin.TranscribeStreamAsync())
{
    Console.WriteLine($"[Direct Result] {result}");
}
