using System.Text.Json;
using System.Text.RegularExpressions;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Services;

public class QueryService
{
    private readonly DatabaseService _databaseService;
    private readonly string _queriesPath;

    public QueryService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _queriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Queries");
    }

    public async Task<(QueryMetadata QueryMetadata, string FileName)> GenerateAndSaveQueryAsync(
        Guid id,
        string friendlyName,
        string description,
        string tableName,
        string[] fields)
    {
        // Sanitize field names
        var sanitizedFields = fields
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f.Trim().Replace(";", "").Replace("--", "").Replace("\"", "").Replace("'", ""))
            .Where(f => !string.IsNullOrEmpty(f) && f.Length > 0)
            .Where(f => Regex.IsMatch(f, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            .ToArray();
        
        if (sanitizedFields.Length == 0)
        {
            throw new ArgumentException("No valid fields provided. Field names must contain only letters, numbers, and underscores, and start with a letter or underscore.");
        }
        
        // Fetch column metadata from database
        var columnMetadataList = await _databaseService.GetColumnMetadataForFieldsAsync(tableName, sanitizedFields);
        
        // Generate SELECT statement
        var fieldList = string.Join(", ", sanitizedFields);
        var sanitizedTableName = tableName.Replace("\"", "").Replace("'", "");
        var sqlQuery = $"SELECT {fieldList} FROM {sanitizedTableName}";
        
        if (sqlQuery.Length > 1000)
        {
            throw new ArgumentException("Generated SQL query is too long. Please select fewer fields.");
        }
        
        // Create query metadata
        var currentTime = DateTime.UtcNow;
        var queryMetadata = new QueryMetadata(
            id,
            friendlyName,
            description ?? string.Empty,
            tableName,
            sanitizedFields,
            columnMetadataList.ToArray(),
            sqlQuery,
            currentTime,
            currentTime
        );
        
        // Ensure Queries directory exists
        if (!Directory.Exists(_queriesPath))
        {
            Directory.CreateDirectory(_queriesPath);
        }
        
        // Save query metadata to JSON file
        var fileName = $"{id}.json";
        var filePath = Path.Combine(_queriesPath, fileName);
        
        var jsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var jsonContent = JsonSerializer.Serialize(queryMetadata, jsonOptions);
        
        await File.WriteAllTextAsync(filePath, jsonContent);
        
        return (queryMetadata, fileName);
    }

    public QueryMetadata? GetQueryById(string id)
    {
        if (!Directory.Exists(_queriesPath))
        {
            return null;
        }
        
        // Try JSON format first
        var jsonFilePath = Path.Combine(_queriesPath, $"{id}.json");
        if (File.Exists(jsonFilePath))
        {
            var jsonContent = File.ReadAllText(jsonFilePath);
            return JsonSerializer.Deserialize<QueryMetadata>(jsonContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        
        // Try legacy TXT format for backward compatibility
        var txtFilePath = Path.Combine(_queriesPath, $"{id}.txt");
        if (File.Exists(txtFilePath))
        {
            var sqlContent = File.ReadAllText(txtFilePath);
            var fileInfo = new FileInfo(txtFilePath);
            
            return new QueryMetadata(
                Guid.TryParse(id, out var guid) ? guid : Guid.Empty,
                $"Legacy Query - {id}",
                "Migrated from legacy format",
                "Unknown",
                Array.Empty<string>(),
                Array.Empty<ColumnMetadata>(),
                sqlContent,
                fileInfo.CreationTime,
                fileInfo.LastWriteTime
            );
        }
        
        return null;
    }

    public List<QueryListItem> GetAllQueries()
    {
        if (!Directory.Exists(_queriesPath))
        {
            return new List<QueryListItem>();
        }
        
        var queryFiles = new List<QueryListItem>();
        
        // Process JSON files (new format)
        var jsonFiles = Directory.GetFiles(_queriesPath, "*.json");
        foreach (var filePath in jsonFiles)
        {
            try
            {
                var jsonContent = File.ReadAllText(filePath);
                var queryMetadata = JsonSerializer.Deserialize<QueryMetadata>(jsonContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (queryMetadata != null)
                {
                    queryFiles.Add(new QueryListItem(
                        Path.GetFileName(filePath),
                        queryMetadata.FriendlyName,
                        queryMetadata.Description,
                        queryMetadata.TableName,
                        queryMetadata.DateAdded,
                        queryMetadata.DateUpdated,
                        queryMetadata.Fields?.Length ?? 0
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading query file {filePath}: {ex.Message}");
            }
        }
        
        // Process legacy TXT files (old format)
        var txtFiles = Directory.GetFiles(_queriesPath, "*.txt");
        foreach (var filePath in txtFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileName(filePath);
                
                queryFiles.Add(new QueryListItem(
                    fileName,
                    $"Legacy Query - {Path.GetFileNameWithoutExtension(fileName)}",
                    "Migrated from legacy format",
                    "Unknown",
                    fileInfo.CreationTime,
                    fileInfo.LastWriteTime,
                    0
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading legacy query file {filePath}: {ex.Message}");
            }
        }
        
        return queryFiles.OrderByDescending(q => q.DateAdded).ToList();
    }

    public (bool Success, string Message, List<string> DeletedFiles) DeleteQuery(string queryId)
    {
        var deletedFiles = new List<string>();
        
        // Validate GUID format
        if (!Guid.TryParse(queryId, out _))
        {
            return (false, $"Invalid query ID format: {queryId}", deletedFiles);
        }
        
        // Delete query JSON file
        var queryFilePath = Path.Combine(_queriesPath, $"{queryId}.json");
        if (File.Exists(queryFilePath))
        {
            File.Delete(queryFilePath);
            deletedFiles.Add($"{queryId}.json");
        }
        
        // Delete associated dashboard file
        var dashboardsPath = Path.Combine(Directory.GetCurrentDirectory(), "Dashboards");
        var dashboardFilePath = Path.Combine(dashboardsPath, $"user_{queryId}.rdash");
        if (File.Exists(dashboardFilePath))
        {
            File.Delete(dashboardFilePath);
            deletedFiles.Add($"user_{queryId}.rdash");
        }
        
        if (deletedFiles.Count == 0)
        {
            return (false, $"Query with ID '{queryId}' not found", deletedFiles);
        }
        
        return (true, $"Successfully deleted query and associated files", deletedFiles);
    }
}
