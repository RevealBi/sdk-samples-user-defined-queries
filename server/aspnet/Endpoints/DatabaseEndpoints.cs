using RevealSdk.Server.Models;
using RevealSdk.Server.Services;

namespace RevealSdk.Server.Endpoints;

public static class DatabaseEndpoints
{
    public static void MapDatabaseEndpoints(this WebApplication app)
    {
        // Get table/view schema from PostgreSQL
        app.MapGet("/api/table-schema/{tableName}", async (string tableName, DatabaseService databaseService) =>
        {
            try
            {
                var columns = await databaseService.GetTableSchemaAsync(tableName);
                
                if (columns.Count == 0)
                {
                    return Results.NotFound($"Table or view '{tableName}' not found or has no accessible columns.");
                }
                
                return Results.Ok(new TableSchemaResponseWithRevealType(tableName, columns.ToArray()));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving schema for table '{tableName}': {ex.Message}");
                return Results.Problem($"An error occurred while retrieving schema for table '{tableName}'.");
            }
        })
        .Produces<TableSchemaResponseWithRevealType>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
