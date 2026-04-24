using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Wolverine & RabbitMQ for PUBLISHING only
builder.Host.UseWolverine(opts =>
{
    // Connect to the local Docker RabbitMQ instance
    opts.UseRabbitMq("amqp://localhost:5672").AutoProvision();

    // Route our specific message to a RabbitMQ queue named "weather_queue"
    opts.PublishMessage<WeatherForecastMessage>().ToRabbitQueue("weather_queue")
    .DeliverWithin(TimeSpan.FromMinutes(1));
});

var app = builder.Build();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async ([FromServices] IMessageBus bus) =>
{
    await bus.PublishAsync(new WeatherForecastMessage
    {
        Message = "test",
        Phonenumber ="12345"
    });

    return new { };
});

app.Run();

[MessageIdentity("weather_forecast")]
public class WeatherForecastMessage()
{
    public string? Phonenumber { get; set; } = "123";

    public string? Message { get; set; } = "Test";
}

