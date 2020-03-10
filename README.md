# Throw context enricher for Serilog
Captures LogContext of thrown exception to enrich logs when the exception is actually logged.

## Problem
Usual logs contain context properties of their scope. If an exception occurs and gets logged in an exception handler, context properties of origin scope are lost.

Consider some asp.net core controller:
```
[HttpGet()]
public WeatherForecast Get()
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
```
and global exception handler middleware:
```
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
