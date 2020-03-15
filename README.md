# Throw context enricher for Serilog
Captures LogContext of a thrown exception to enrich logs when the exception is eventually logged.

## Use case
Assume an exception is thrown in scope with context properties:
```c#
[HttpGet()]
public WeatherForecast Get()
{
    var rng = new Random();
    using (LogContext.PushProperty("WeatherForecast", new
    {
        Date = DateTime.Now.AddDays(1),
        TemperatureC = rng.Next(-20, 55),
        Summary = Summaries[rng.Next(Summaries.Length)]
    }))
    {
        throw new Exception("Oops");
    }
}
```
If the exception is caught and logged outside the scope, context properties are lost by default.
```c#
// global exception handler middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Exceptional weather"); // would not contain WeatherForecast property`
    }
});

```

## Installation
It is possible to register ThrowContextEnricher globally or just to use once in a specific exception handler.

### Global enricher
Just add `.Enrich.With<ThrowContextEnricher>()`:

```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    .WriteTo
    ...
    .CreateLogger();
```

Note that order of `ThrowContextEnricher` and Serilog's `LogContextEnricher` matters when a property exists both in exception's origin and handler contexts:

<table> 
    <tr>
        <td>
<pre lang="c#">
Log.Logger = new LoggerConfiguration()
    .Enrich.With&lt;ThrowContextEnricher&gt;()
    .Enrich.FromLogContext()
    ...
</pre>
        </td>
        <td>
<pre lang="c#">
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.With&lt;ThrowContextEnricher&gt;()
    ...
</pre>    
    </tr>
    <tr>
        <td colspan="2">
<pre lang="c#">
using (LogContext.PushProperty("A", "outer"))
    try
    {
        using (LogContext.PushProperty("A", "inner"))
            throw new Exception();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Value of A is {A}");
    }
</pre>
        </td>
    </tr>
    <tr>
        <td>Exception's value is used (A="inner")</td>
        <td>Handler's value is used (A="outer")</td>
    </tr>
</table>

### Local enricher
It is also possible to enrich only specific log rather registering the enricher globally:

```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    ...
    .CreateLogger();

ThrowContextEnricher.EnsureInitialized();

...
using (LogContext.PushProperty("A", "outer"))
    try
    {
        using (LogContext.PushProperty("A", "inner"))
        {
            throw new Exception();
        }

    }
    catch (Exception ex)
    {
        using (LogContext.Push(new ThrowContextEnricher()))
        {
            Log.Error(ex, "Value of A is {A}"); // "Value of A is \"inner\""
        }
    }
```

Note `ThrowContextEnricher.EnsureInitialized()` is used to trigger ThrowContextEnricher' static ctor to begin capturing properties. If this call is omitted, enricher may be lazily initialized only in an exception handler, thus the first occurred exception would miss properties.

## Rethrow
If an exception is rethrown in a different context, the origin property value is not overwritten:
```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

try
{
    try
    {
        using (LogContext.PushProperty("A", 1))
            throw new ApplicationException();
    }
    catch
    {
        using (LogContext.PushProperty("A", 2))
        using (LogContext.PushProperty("B", 2))
            throw;
    }
}
catch (ApplicationException ex)
{
    Log.Information(ex, "A={A}, B={B}"); // "A=1, B=2"
}
```

## Wrap
If an exception is wrapped into another in a different context, the wrapper context is used. Log of inner exception produces origin value though.

```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

try
{
    try
    {
        using (LogContext.PushProperty("A", 1))
            throw new Exception();
    }
    catch (Exception ex)
    {
        using (LogContext.PushProperty("A", 2))
        using (LogContext.PushProperty("B", 2))
            throw new ApplicationException("Wrapper", ex);
    }
}
catch (ApplicationException ex)
{
    Log.Information(ex, "A={A}, B={B}"); // "A=2, B=2"
    Log.Information(ex.InnerException, "A={A}, B={B}"); // "A=1, B={B}"
}
```
