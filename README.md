# Throw context enricher for Serilog
Captures LogContext of thrown exception to enrich logs when the exception is actually logged.

## Problem
Withing a scope logger writes context properties. If an exception occurs and gets logged in an exception handler, context properties are lost.
```
// some asp.net core controller
[HttpGet()]
public IEnumerable<WeatherForecast> Get()
{
    var rng = new Random();
    using (LogContext.Push(new PropertyEnricher("WeatherForecast", new
    {
        Date = DateTime.Now.AddDays(1),
        TemperatureC = rng.Next(-20, 55),
        Summary = Summaries[rng.Next(Summaries.Length)]
    })))
    {
        _logger.LogInformation("Today Weather forecast");

        throw new Exception("Oops");
    }
}

// global exception handler middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Exceptional weather");
    }
});

```

In code above only the first log gets context properties.

## Solution
Save context on `CurrentDomain.FirstChanceException` event and enrich an exception handler.
