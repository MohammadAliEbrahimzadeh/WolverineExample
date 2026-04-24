using Consumer;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(logging => logging.AddConsole());

builder.Services.AddHostedService<DeadLetterJob>();

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq("amqp://localhost:5672").EnableEnhancedDeadLettering();

    // Weather queue with DLQ
    opts.ListenToRabbitQueue("weather_queue");

    // Email queue with DLQ
    opts.ListenToRabbitQueue("email_queue");
});

var app = builder.Build();
Console.WriteLine("Listening on weather_queue and email_queue...");
await app.RunAsync();

// Message Classes - MUST match Producer exactly (including MessageIdentity)
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

// Weather Handlers
public class WeatherHandler
{
    [RetryNow(typeof(Exception), 3)]
    public void Handle(WeatherForecastMessage message, ILogger<WeatherHandler> logger)
    {
        logger.LogInformation("Processing SMS to {Phone}", message.PhoneNumber);
        throw new InvalidOperationException("SMS service unavailable");
    }
}

public class WeatherDeadLetterHandler
{
    public void Handle(WeatherForecastMessage message, Envelope envelope, ILogger<WeatherDeadLetterHandler> logger)
    {
        // Verify this came through DLX (has x-death header)
        if (!envelope.Headers.TryGetValue("x-death", out var deathInfo))
        {
            logger.LogWarning("Message received on DLQ without x-death header");
            return;
        }

        logger.LogError("SMS DEAD LETTER: Phone={Phone}, DeathInfo={Death}",
            message.PhoneNumber,
            deathInfo);

        // TODO: Save to DB, send alert, etc.
        // DO NOT THROW HERE or message returns to DLQ
    }
}

// Email Handlers
public class EmailHandler
{
    public void Handle(EmailMessage message, ILogger<EmailHandler> logger)
    {
        logger.LogInformation("Processing Email to {To}", message.To);
        throw new InvalidOperationException("Email service unavailable");
    }
}

public class EmailDeadLetterHandler
{
    public void Handle(EmailMessage message, Envelope envelope, ILogger<EmailDeadLetterHandler> logger)
    {
        if (!envelope.Headers.TryGetValue("x-death", out var deathInfo))
        {
            logger.LogWarning("Message received on DLQ without x-death header");
            return;
        }

        logger.LogError("EMAIL DEAD LETTER: To={To}, Subject={Subject}, DeathInfo={Death}",
            message.To,
            message.Subject,
            deathInfo);

        // TODO: Save to DB, send alert, etc.
        // DO NOT THROW HERE
    }
}
