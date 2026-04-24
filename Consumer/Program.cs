using Wolverine;
using Wolverine.Attributes;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// This is a good place to add ILogger, etc.
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});


// 1. Configure Wolverine to LISTEN only
builder.Host.UseWolverine(opts =>
{
    // Connect to the local Docker RabbitMQ instance
    opts.UseRabbitMq("amqp://localhost:5672");

    // Tell Wolverine to listen to this queue.
    // Wolverine will automatically create the queue if it does not exist.
    opts.ListenToRabbitQueue("weather_queue");

    opts.Policies.OnException<Exception>().RetryTimes(3);
});

var app = builder.Build();

// We don't need any endpoints, so we just run the host.
// It will run as a background service listening for messages.
Console.WriteLine("Wolverine consumer is running. Waiting for messages on 'weather_queue'...");
Console.WriteLine("Press CTRL+C to exit.");
await app.RunAsync();

[MessageIdentity("weather_forecast")]
public class WeatherForecastMessage()
{
    public string? Phonenumber { get; set; } = "123";

    public string? Message { get; set; } = "Test";
}

// No interfaces required!
public class WeatherRequestedHandler
{
    // Wolverine automatically maps WeatherForecastMessage to this method 
    // because of the parameter type.
    public void Handle(WeatherForecastMessage message, ILogger<WeatherRequestedHandler> logger)
    {
        throw new NotImplementedException();
    }
}

