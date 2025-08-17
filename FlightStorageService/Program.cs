using FlightStorageService.Middleware;
using FlightStorageService.Repositories;
using FlightStorageService.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Flight Information API",
        Version = "v1",
        Description = "API для пошуку та додавання рейсів. Повертає ProblemDetails для помилок."
    });
    o.EnableAnnotations();
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath))
        o.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    o.DescribeAllParametersInCamelCase();
    o.SupportNonNullableReferenceTypes();
});
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("flights", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync("""
        { "title":"Too Many Requests", "status":429, "detail":"Rate limit exceeded. Try again later." }
        """, token);
    };
});

// Http logging
builder.Services.AddHttpLogging(o => {
    o.LoggingFields = HttpLoggingFields.RequestMethod |
                      HttpLoggingFields.RequestPath |
                      HttpLoggingFields.ResponseStatusCode |
                      HttpLoggingFields.Duration;
});

builder.Services.AddCors(o =>
{
    o.AddPolicy("ui", p => p
        .WithOrigins("https://localhost:7193", "http://localhost:5275")
        //.WithOrigins("https://flightclientapp:8080", "http://flightclientapp:8080")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// DI
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

var app = builder.Build();
// перед app.Run();

app.UseMiddleware<ProblemDetailsMiddleware>();

app.UseRateLimiter();

app.UseHttpLogging();

//if (app.Environment.IsEnvironment("Docker"))
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
//        var connectionString = config.GetConnectionString("FlightsDb");

//        using var connection = new SqlConnection(connectionString);
//        await connection.OpenAsync();

//        string sql = File.ReadAllText("Scripts/init.sql");
//        using var command = new SqlCommand(sql, connection);
//        await command.ExecuteScalarAsync();
//    }
//}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Information API v1");
        ui.RoutePrefix = "swagger";
    });
}

app.UseCors("ui");
app.MapControllers();

if (!app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Docker")
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Run();
