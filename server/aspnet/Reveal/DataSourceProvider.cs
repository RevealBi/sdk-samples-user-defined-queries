using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.PostgreSQL;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Reveal
{
    internal class DataSourceProvider : IRVDataSourceProvider
    {
        public Task<RVDashboardDataSource> ChangeDataSourceAsync(IRVUserContext userContext, RVDashboardDataSource dataSource)
        {
            if (dataSource is RVPostgresDataSource SqlDs)
            {
                SqlDs.Host = (string)userContext.Properties["Host"];
                SqlDs.Database = (string)userContext.Properties["Database"];
            }
            return Task.FromResult(dataSource);
        }

        public Task<RVDataSourceItem>? ChangeDataSourceItemAsync(IRVUserContext userContext, string dashboardId, RVDataSourceItem dataSourceItem)
        {
            // ****
            // Every request for data passes thru changeDataSourceItem
            // You can set query properties based on the incoming requests
            // for example, you can check:
            // - dsi.Id
            // - dsi.Table
            // - dsi.FunctionName
            // - dsi.Title
            // and take a specific action on the dsi as this request is processed
            // ****

            if (dataSourceItem is not RVPostgresDataSourceItem sqlDsi) return Task.FromResult(dataSourceItem);

            ChangeDataSourceAsync(userContext, sqlDsi.DataSource);

            // Check if the sqlDsi.Id is a GUID and load query from file, if it is not a GUID,
            // we can assume to process the request as normal, not loading from file
            if (!string.IsNullOrEmpty(sqlDsi.Id) && Guid.TryParse(sqlDsi.Id, out Guid queryId))
            {
                try
                {
                    string queriesPath = Path.Combine(Directory.GetCurrentDirectory(), "Queries");                    
                    string jsonQueryFilePath = Path.Combine(queriesPath, $"{queryId}.json");
                    if (File.Exists(jsonQueryFilePath))
                    {
                        string jsonContent = File.ReadAllText(jsonQueryFilePath);
                        var queryData = System.Text.Json.JsonSerializer.Deserialize<QueryMetadata>(jsonContent, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (queryData != null && !string.IsNullOrEmpty(queryData.Query))
                        {
                            sqlDsi.CustomQuery = queryData.Query;
                            Console.WriteLine($"Loaded custom query from JSON file: {jsonQueryFilePath}");
                            Console.WriteLine($"Query: {queryData.Query}");
                        }
                    }
                    else
                    {
                        // Fall back to legacy TXT format for backward compatibility
                        string txtQueryFilePath = Path.Combine(queriesPath, $"{queryId}.txt");
                        if (File.Exists(txtQueryFilePath))
                        {
                            string customQuery = File.ReadAllText(txtQueryFilePath);
                            sqlDsi.CustomQuery = customQuery;
                            Console.WriteLine($"Loaded custom query from legacy TXT file: {txtQueryFilePath}");
                            Console.WriteLine($"Query: {customQuery}");
                        }
                        else
                        {
                            Console.WriteLine($"Query file not found: {jsonQueryFilePath} or {txtQueryFilePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading query file for GUID {queryId}: {ex.Message}");
                }
            }
          
            return Task.FromResult(dataSourceItem);
        }
    }
}