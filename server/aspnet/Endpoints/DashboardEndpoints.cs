using Reveal.Sdk;
using RevealSdk.Server.Models;
using RevealSdk.Server.Services;

namespace RevealSdk.Server.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        // Generate a Grid dashboard from a query GUID
        app.MapPost("/api/generate-grid-dashboard/{queryId}", async (string queryId, DashboardService dashboardService) =>
        {
            try
            {
                var (dashboardId, fileName, title) = await dashboardService.GenerateGridDashboardAsync(queryId);
                
                return Results.Ok(new 
                { 
                    DashboardId = dashboardId,
                    FileName = fileName,
                    Title = title,
                    Message = "Grid dashboard generated successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating grid dashboard for query '{queryId}': {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Results.Problem($"An error occurred while generating the grid dashboard: {ex.Message}");
            }
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        // Get dashboard thumbnail information
        app.MapGet("/dashboards/{name}/thumbnail", async (string name) =>
        {
            var path = "dashboards/" + name + ".rdash";
            if (File.Exists(path))
            {
                var dashboard = new Dashboard(path);
                var info = await dashboard.GetInfoAsync(Path.GetFileNameWithoutExtension(path));
                return TypedResults.Ok(info);
            }
            else
            {
                return Results.NotFound();
            }
        });

        // Get dashboard names and titles
        app.MapGet("/dashboards/names", (DashboardService dashboardService) =>
        {
            try
            {
                var fileNames = dashboardService.GetDashboardNames();
                return Results.Ok(fileNames);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Reading Directory : {ex.Message}");
                return Results.Problem("An unexpected error occurred while processing the request.");
            }
        })
        .Produces<IEnumerable<DashboardNames>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
