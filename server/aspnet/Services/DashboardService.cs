using System.Text.Json;
using Reveal.Sdk.Dom;
using Reveal.Sdk.Dom.Data;
using Reveal.Sdk.Dom.Visualizations;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Services;

public class DashboardService
{
    private readonly string _dashboardsPath;
    private readonly string _queriesPath;

    public DashboardService()
    {
        _dashboardsPath = Path.Combine(Directory.GetCurrentDirectory(), "Dashboards");
        _queriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Queries");
    }

    public async Task<(string DashboardId, string FileName, string Title)> GenerateGridDashboardAsync(string queryId)
    {
        // Validate GUID format
        if (!Guid.TryParse(queryId, out _))
        {
            throw new ArgumentException("Invalid query ID format. Expected a valid GUID.");
        }
        
        // Load query metadata
        var jsonFilePath = Path.Combine(_queriesPath, $"{queryId}.json");
        
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException($"Query with ID '{queryId}' not found.");
        }
        
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var queryMetadata = JsonSerializer.Deserialize<QueryMetadata>(jsonContent, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (queryMetadata == null || queryMetadata.Columns == null || queryMetadata.Columns.Length == 0)
        {
            throw new InvalidOperationException("Query metadata is invalid or missing column information.");
        }
        
        // Create the RdashDocument
        var title = queryMetadata.FriendlyName;
        var document = new RdashDocument(title);
        
        // Create a simple DataSource
        var postgresDataSource = new PostgreSQLDataSource()
        {
            Title = $"PostgreSQL Database"
        };
        
        // Create a DataSourceItem referencing the query by its GUID
        var dataSourceItem = new DataSourceItem(title, postgresDataSource)
        {
            Id = queryId.ToString(),
            Title = title,
            Subtitle = queryMetadata.Description ?? $"Data from {queryMetadata.TableName}",
            Fields = DataTypeMapper.MapColumnsToRevealFields(queryMetadata.Columns)
        };
        
        // Create a GridVisualization bound to that DataSourceItem
        var gridVisualization = new GridVisualization("Dynamic Grid", dataSourceItem)
        {
            ColumnSpan = 3,
            RowSpan = 4,
            Description = queryMetadata.Description ?? $"Grid visualization for {queryMetadata.TableName}",
            IsTitleVisible = true,
            Id = queryId.ToString(),
            Title = title
        };
        
        // Configure Grid settings
        gridVisualization.ConfigureSettings(settings =>
        {
            settings.FontSize = FontSize.Small;
            settings.PageSize = 30;
            settings.IsPagingEnabled = true;
            settings.IsFirstColumnFixed = true;
        });
        
        // Set which columns to display in the grid
        gridVisualization.SetColumns(queryMetadata.Fields);
        
        // Add the visualization to the dashboard document
        document.Visualizations.Add(gridVisualization);
        
        // Save the document
        if (!Directory.Exists(_dashboardsPath))
        {
            Directory.CreateDirectory(_dashboardsPath);
        }
        
        var dashboardFileName = $"user_{queryId}.rdash";
        var dashboardFilePath = Path.Combine(_dashboardsPath, dashboardFileName);
        document.Save(dashboardFilePath);
        
        return ($"user_{queryId}", dashboardFileName, title);
    }

    public List<DashboardNames> GetDashboardNames()
    {
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Dashboards");
        
        if (!Directory.Exists(folderPath))
        {
            return new List<DashboardNames>();
        }
        
        var files = Directory.GetFiles(folderPath);
        
        var fileNames = files.Select(file =>
        {
            try
            {
                return new DashboardNames
                {
                    DashboardFileName = Path.GetFileNameWithoutExtension(file),
                    DashboardTitle = RdashDocument.Load(file).Title
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Reading FileData {file}: {ex.Message}");
                return null;
            }
        }).Where(fileData => fileData != null)
          .Cast<DashboardNames>()
          .ToList();

        return fileNames;
    }
}
