using RevealSdk.Server.Models;
using RevealSdk.Server.Services;

namespace RevealSdk.Server.Endpoints;

public static class QueryEndpoints
{
    public static void MapQueryEndpoints(this WebApplication app)
    {
        // Generate SQL query and save to Queries folder with metadata
        app.MapPost("/api/generate-query", async (GenerateQueryRequest request, QueryService queryService) =>
        {
            try 
            {
                if (request.Fields == null || request.Fields.Length == 0)
                {
                    return Results.BadRequest("Fields array cannot be empty.");
                }
                
                if (string.IsNullOrWhiteSpace(request.TableName))
                {
                    return Results.BadRequest("Table name is required.");
                }

                if (string.IsNullOrWhiteSpace(request.FriendlyName))
                {
                    return Results.BadRequest("Friendly name is required.");
                }
                
                var (queryMetadata, fileName) = await queryService.GenerateAndSaveQueryAsync(
                    request.Id,
                    request.FriendlyName,
                    request.Description,
                    request.TableName,
                    request.Fields
                );
                
                return Results.Ok(new 
                { 
                    Id = request.Id,
                    FileName = fileName,
                    FriendlyName = request.FriendlyName,
                    Query = queryMetadata.Query,
                    Message = "Query generated and saved successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating query for ID '{request.Id}': {ex.Message}");
                return Results.Problem("An error occurred while generating the query.");
            }
        })
        .Accepts<GenerateQueryRequest>("application/json")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get a specific query by ID
        app.MapGet("/api/queries/{id}", (string id, QueryService queryService) =>
        {
            try
            {
                var queryMetadata = queryService.GetQueryById(id);
                
                if (queryMetadata == null)
                {
                    return Results.NotFound($"Query with ID '{id}' not found");
                }
                
                return Results.Ok(queryMetadata);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving query '{id}': {ex.Message}");
                return Results.Problem($"An error occurred while retrieving query '{id}'.");
            }
        })
        .Produces<QueryMetadata>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // List all saved query files with metadata
        app.MapGet("/api/queries", (QueryService queryService) =>
        {
            try
            {
                var queries = queryService.GetAllQueries();
                return Results.Ok(queries);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving query files: {ex.Message}");
                return Results.Problem("An error occurred while retrieving query files.");
            }
        })
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Delete a query by ID (also deletes associated dashboard)
        app.MapDelete("/api/queries/{id}", (string id, QueryService queryService) =>
        {
            try
            {
                var result = queryService.DeleteQuery(id);
                
                if (!result.Success)
                {
                    return Results.NotFound(result.Message);
                }
                
                return Results.Ok(new { Message = result.Message, DeletedFiles = result.DeletedFiles });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting query '{id}': {ex.Message}");
                return Results.Problem($"An error occurred while deleting query '{id}'.");
            }
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}