using Confluent.Kafka;
using FlightStorageService.Caching;
using FlightStorageService.Middleware;
using FlightStorageService.Models;
using FlightStorageService.Repositories;
using FlightStorageService.Services;
using KafkaExample.Services;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

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
    {
        o.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
    o.DescribeAllParametersInCamelCase();
    o.SupportNonNullableReferenceTypes();
});

builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("flights", httpContext =>
    {
        string partitionKey;

        if (httpContext.Connection.RemoteIpAddress != null)
        {
            partitionKey = httpContext.Connection.RemoteIpAddress.ToString();
        }
        else
        {
            partitionKey = ClientTypeMode.Anonimous;
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        ctx.HttpContext.Response.ContentType = "application/json";

        string responseJson = "{ \"title\":\"Too Many Requests\", \"status\":429, \"detail\":\"Rate limit exceeded. Try again later.\" }";
        await ctx.HttpContext.Response.WriteAsync(responseJson, token);
    };
});

builder.Services.Configure<CorsSettings>(
    builder.Configuration.GetSection("CorsSettings"));

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;
});

builder.Services.AddCors(options =>
{
    var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();

    options.AddPolicy("ui", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// DI
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightService, FlightService>();

if (builder.Configuration.GetValue<bool>("Kafka:Enabled"))
{
    builder.Services.AddHostedService<KafkaConsumerService>();
}

builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

var cacheMode = builder.Configuration["Cache:Mode"] ?? builder.Configuration["Cache__Mode"] ?? "Redis";
if (string.Equals(cacheMode, "Redis", StringComparison.OrdinalIgnoreCase))
{
    var connStr = builder.Configuration["Redis:Connection"] ?? builder.Configuration["Redis__Connection"]!;
    var mux = await ConnectionMultiplexer.ConnectAsync(connStr);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);

    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.Configuration = connStr;
        o.InstanceName = builder.Configuration["Redis:InstanceName"]
                         ?? builder.Configuration["Redis__InstanceName"]
                         ?? "fis:";
    });

    builder.Services.AddSingleton<IAppCache, RedisAppCache>();
}
else
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton<IAppCache, InMemoryAppCache>();
}
// ...

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));


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
