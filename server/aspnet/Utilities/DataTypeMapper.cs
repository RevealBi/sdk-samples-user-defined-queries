using Reveal.Sdk.Dom;
using Reveal.Sdk.Dom.Data;
using Reveal.Sdk.Dom.Visualizations;
using RevealSdk.Server.Models;

namespace RevealSdk.Server.Services;

public static class DataTypeMapper
{
    public static string MapPostgresDataTypeToRevealDataType(string dataType)
    {
        return dataType.ToLower() switch
        {
            "character varying" or "varchar" or "char" or "character" or "text" or "uuid" => "String",
            "smallint" or "integer" or "int" or "bigint" or "smallserial" or "serial" or "bigserial" => "Number",
            "numeric" or "decimal" or "real" or "double precision" or "money" => "Number",
            "date" or "timestamp" or "timestamp without time zone" or "timestamp with time zone" or "timestamptz" => "Date",
            "time" or "time without time zone" or "time with time zone" => "Time",
            "boolean" or "bool" => "Boolean",
            _ => "String"
        };
    }

    public static List<IField> MapColumnsToRevealFields(ColumnMetadata[] columns)
    {
        var result = new List<IField>();
        foreach (var col in columns)
        {
            switch (col.RevealDataType)
            {
                case "Number":
                    result.Add(new NumberField(col.ColumnName) { FieldLabel = col.ColumnName });
                    break;
                case "Date":
                    result.Add(new DateField(col.ColumnName) { FieldLabel = col.ColumnName });
                    break;
                case "Time":
                    result.Add(new DateField(col.ColumnName) { FieldLabel = col.ColumnName });
                    break;
                case "Boolean":
                    result.Add(new TextField(col.ColumnName) { FieldLabel = col.ColumnName });
                    break;
                default:
                    result.Add(new TextField(col.ColumnName) { FieldLabel = col.ColumnName });
                    break;
            }
        }
        return result;
    }
}
