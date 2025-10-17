namespace RevealSdk.Server.Models
{
    public record ColumnMetadata(
        string ColumnName,
        string DataType,
        string RevealDataType
    );

    public record QueryMetadata(
        Guid Id,
        string FriendlyName,
        string Description,
        string TableName,
        string[] Fields,
        ColumnMetadata[]? Columns,
        string Query,
        DateTime DateAdded,
        DateTime DateUpdated
    );

    public record QueryListItem(
        string FileName,
        string FriendlyName,
        string Description,
        string TableName,
        DateTime DateAdded,
        DateTime DateUpdated,
        int FieldCount
    );
}
