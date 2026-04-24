using Microsoft.AspNetCore.Mvc;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq("amqp://localhost:5672").AutoProvision();

    // Weather pipeline
    opts.PublishMessage<WeatherForecastMessage>()
        .ToRabbitQueue("weather_queue")
        .DeliverWithin(TimeSpan.FromMinutes(1));

    // Email pipeline  
    opts.PublishMessage<EmailMessage>()
        .ToRabbitQueue("email_queue")
        .DeliverWithin(TimeSpan.FromMinutes(1));
});

var app = builder.Build();
app.UseHttpsRedirection();

// Publish Weather
app.MapPost("/weather", async (IMessageBus bus, [FromBody] WeatherRequest request) =>
{
    await bus.PublishAsync(new WeatherForecastMessage
    {
        PhoneNumber = request.Phone,
        Message = request.Text
    });
    return Results.Accepted();
});

// Publish Email
app.MapPost("/email", async (IMessageBus bus, [FromBody] EmailRequest request) =>
{
    await bus.PublishAsync(new EmailMessage
    {
        To = request.To,
        Subject = request.Subject,
        Body = request.Body
    });
    return Results.Accepted();
});

app.Run();

// Message Classes - MUST match exactly on Consumer side
[MessageIdentity("weather_forecast")]
public class WeatherForecastMessage
{
    public string PhoneNumber { get; set; } = default!;
    public string Message { get; set; } = default!;
}

[MessageIdentity("email_notification")]
public class EmailMessage
{
    public string To { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
}

// Request DTOs
public record WeatherRequest(string Phone, string Text);
public record EmailRequest(string To, string Subject, string Body);
