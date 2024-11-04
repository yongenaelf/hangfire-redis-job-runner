using System.Diagnostics;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Shared;

var builder = WebApplication.CreateBuilder(args);

// get connection string from configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

// Add Hangfire services
builder.Services.AddHangfire(configuration =>
    configuration
        .UseRedisStorage(redisConnectionString)
        .WithJobExpirationTimeout(TimeSpan.FromMinutes(1))
    );

// add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use Hangfire Dashboard for managing jobs
app.UseHangfireDashboard();

// Use Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Test endpoint
app.MapGet("/", () => "Hangfire Enqueuer Running");

app.MapPost("/build", (IBackgroundJobClient backgroundJobs, IFormFile file) =>
{
    // convert the file to base64
    using (var memoryStream = new MemoryStream())
    {
        file.CopyTo(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var base64 = Convert.ToBase64String(fileBytes);

        // Enqueue the job
        var jobId = backgroundJobs.Enqueue<Build>((x) => x.DotnetBuild(base64));

        string? result = null;
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (result == null)
        {
            // get job return value
            IMonitoringApi jobMonitoringApi = JobStorage.Current.GetMonitoringApi();
            JobDetailsDto job = jobMonitoringApi.JobDetails(jobId);

            // check if the job is completed
            if (job.History[0].StateName != "Succeeded")
            {
                // wait for the job to complete
                Thread.Sleep(1000);
                continue;
            }

            result = job.History[0].Data["Result"];
        }

        stopwatch.Stop();
        Console.WriteLine($"Job completed in {stopwatch.ElapsedMilliseconds}ms");

        return result;
    }
}).DisableAntiforgery();

app.Run();