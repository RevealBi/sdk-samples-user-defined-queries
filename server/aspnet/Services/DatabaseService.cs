using Npgsql;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;

    public DatabaseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        var server = _configuration["Server:Host"];
        var database = _configuration["Server:Database"];
        var username = _configuration["Server:Username"];
        var password = _configuration["Server:Password"];
        var port = _configuration["Server:Port"];
        
        return $"Host={server};Port={port};Database={database};Username={username};Password={password};";
    }

    public async Task<List<ColumnSchemaWithRevealType>> GetTableSchemaAsync(string tableName)
    {
        var connectionString = GetConnectionString();
        var columns = new List<ColumnSchemaWithRevealType>();
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT 
                column_name,
                data_type,
                is_nullable,
                character_maximum_length
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            AND table_schema = @schema
            ORDER BY ordinal_position";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName.ToLower());
        command.Parameters.AddWithValue("@schema", _configuration["Server:Schema"] ?? "public");
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dataType = reader.GetString(1);
            columns.Add(new ColumnSchemaWithRevealType(
                reader.GetString(0), // column_name
                dataType, // data_type
                reader.GetString(2) == "YES", // is_nullable
                reader.IsDBNull(3) ? null : reader.GetInt32(3), // character_maximum_length
                DataTypeMapper.MapPostgresDataTypeToRevealDataType(dataType) // revealDataType
            ));
        }
        
        return columns;
    }

    public async Task<List<ColumnMetadata>> GetColumnMetadataForFieldsAsync(string tableName, string[] fields)
    {
        var connectionString = GetConnectionString();
        var columnMetadataList = new List<ColumnMetadata>();
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var query = @"
            SELECT 
                column_name,
                data_type
            FROM information_schema.columns 
            WHERE table_name = @tableName 
            AND table_schema = @schema
            ORDER BY ordinal_position";
        
        using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName.ToLower());
        command.Parameters.AddWithValue("@schema", _configuration["Server:Schema"] ?? "public");
        
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            
            // Only include columns that are in the selected fields
            if (fields.Contains(columnName))
            {
                columnMetadataList.Add(new ColumnMetadata(
                    columnName,
                    dataType,
                    DataTypeMapper.MapPostgresDataTypeToRevealDataType(dataType)
                ));
            }
        }
        
        return columnMetadataList;
    }
}
