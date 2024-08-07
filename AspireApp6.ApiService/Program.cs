using System.Diagnostics.Metrics;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

//Add custom meter for metrics
var weatherForecastMeter = new Meter("weather.backend", "1.0.0");
var weatherForecastCount = weatherForecastMeter.CreateCounter<int>("forecast.count", description: "Counts the number of weather forecasts made");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("weather.backend");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    // Create a custom activity for the work that we are doing formulating a weather forecast
    using var weatherSpan = greeterActivitySource.StartActivity("ForecastActivity");

    var forecast = new WeatherForecast[5];
    for (int i = 0; i < forecast.Length; i++)
    {
        // Increment the custom counter
        weatherForecastCount.Add(1);

        forecast[i] = new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(i)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        );

        // Add a tag to the Activity/span
        weatherSpan?.SetTag(forecast[i].Date.ToShortDateString(), forecast[i].Summary);
    }

    // Log a message
    logger.LogInformation("Sending weather forecast. {forecast}", forecast);

    return forecast;

});

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
