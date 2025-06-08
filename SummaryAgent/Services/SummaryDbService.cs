using MySql.Data.MySqlClient;

namespace SummaryAgent.Services;

public class SummaryDbService
{
    private readonly string _connStr = "server=localhost;user=root;password=pass;database=chatdb;";

    public async Task SaveSummaryAsync(string user, string summary)
    {
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("INSERT INTO summary (user, summary) VALUES (@user, @summary)", conn);
        cmd.Parameters.AddWithValue("@user", user);
        cmd.Parameters.AddWithValue("@summary", summary);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> GetAllSummariesAsync()
    {
        var summaries = new List<string>();
        using var conn = new MySqlConnection(_connStr);
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("SELECT summary FROM summary", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int summaryIndex = reader.GetOrdinal("summary");
            summaries.Add(reader.GetString(summaryIndex));
        }
        return summaries;
    }
}