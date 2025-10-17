using System.Text.Json;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Endpoints;

public static class SchemaEndpoints
{
    public static void MapSchemaEndpoints(this WebApplication app)
    {
        // Get allowed tables from AllowedTables.json
        app.MapGet("/api/allowed-tables", () =>
        {
            try
            {
                var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "Schema", "AllowedTables.json");
                if (!File.Exists(schemaPath))
                {
                    return Results.NotFound("AllowedTables.json file not found");
                }
                
                var jsonContent = File.ReadAllText(schemaPath);
                var allowedTablesResponse = JsonSerializer.Deserialize<AllowedTablesResponse>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                return Results.Ok(allowedTablesResponse?.AllowedTables ?? Array.Empty<AllowedTable>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading AllowedTables.json: {ex.Message}");
                return Results.Problem("An error occurred while retrieving allowed tables.");
            }
        })
        .Produces<AllowedTable[]>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
