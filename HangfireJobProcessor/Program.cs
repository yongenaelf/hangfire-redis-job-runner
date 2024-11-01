using Hangfire;
using Hangfire.Redis.StackExchange;

var builder = WebApplication.CreateBuilder(args);

// get connection string from configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// Add Hangfire services
builder.Services.AddHangfire(configuration =>
    configuration.UseRedisStorage(redisConnectionString));

// Add the Hangfire server
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);

var app = builder.Build();

// Use Hangfire Dashboard for managing jobs
app.UseHangfireDashboard();

// Test endpoint
app.MapGet("/", () => "Hangfire Job Processor Running");

app.Run();