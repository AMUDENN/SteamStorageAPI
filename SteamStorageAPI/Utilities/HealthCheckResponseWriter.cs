using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SteamStorageAPI.Utilities;

public static class HealthCheckResponseWriter
{
    public static async Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        JsonWriterOptions options = new() { Indented = true };
        using MemoryStream memoryStream = new();
        await using Utf8JsonWriter jsonWriter = new(memoryStream, options);
        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("status", healthReport.Status.ToString());
        jsonWriter.WriteString("totalDuration", healthReport.TotalDuration.ToString());
        jsonWriter.WriteStartObject("entries");

        foreach (KeyValuePair<string, HealthReportEntry> healthReportEntry in healthReport.Entries)
        {
            jsonWriter.WriteStartObject(healthReportEntry.Key);

            jsonWriter.WriteStartObject("data");
            foreach (KeyValuePair<string, object> item in healthReportEntry.Value.Data)
            {
                jsonWriter.WritePropertyName(item.Key);
                JsonSerializer.Serialize(jsonWriter, item.Value, item.Value.GetType());
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteString("duration", healthReportEntry.Value.Duration.ToString());
            jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
            jsonWriter.WriteString("description", healthReportEntry.Value.Description);

            jsonWriter.WriteStartArray("tags");
            foreach (string tag in healthReportEntry.Value.Tags)
                jsonWriter.WriteStringValue(tag);
            jsonWriter.WriteEndArray();

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.WriteEndObject();
        await jsonWriter.FlushAsync();

        await context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
    }
}
