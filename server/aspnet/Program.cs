using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Data.PostgreSQL;
using RevealSdk.Server.Configuration;
using RevealSdk.Server.Reveal;
using RevealSdk.Server.Services;
using RevealSdk.Server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddReveal( builder =>
{
    builder.AddAuthenticationProvider<AuthenticationProvider>();
    builder.AddDataSourceProvider<RevealSdk.Server.Reveal.DataSourceProvider>();
   // builder.AddDashboardProvider<DashboardProvider>();
    builder.AddUserContextProvider<UserContextProvider>();
    builder.DataSources.RegisterPostgreSQL();
});

// Configure application services
builder.Services.Configure<ServerOptions>(
    builder.Configuration.GetSection("Server"));

// Register custom services
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<QueryService>();
builder.Services.AddSingleton<DashboardService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map API endpoints
app.MapSchemaEndpoints();
app.MapDatabaseEndpoints();
app.MapQueryEndpoints();
app.MapDashboardEndpoints();

// Map controllers
app.MapControllers();

app.Run();

// API Response Models
public record AllowedTable(string Name, string Type, string DisplayName, string Description);
public record AllowedTablesResponse(AllowedTable[] AllowedTables);
public record ColumnSchema(string ColumnName, string DataType, bool IsNullable, int? MaxLength);
public record TableSchemaResponse(string TableName, ColumnSchema[] Columns);
public record ColumnSchemaWithRevealType(string ColumnName, string DataType, bool IsNullable, int? MaxLength, string RevealDataType);
public record TableSchemaResponseWithRevealType(string TableName, ColumnSchemaWithRevealType[] Columns);

public record GenerateQueryRequest(
    Guid Id,
    string FriendlyName,
    string Description,
    string TableName,
    string[] Fields
);

public class DashboardNames
{
    public string? DashboardFileName { get; set; }
    public string? DashboardTitle { get; set; }
}
