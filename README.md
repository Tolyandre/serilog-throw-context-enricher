# Throw context enricher for Serilog [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/tolyandre/serilog-throw-context-enricher/1)](https://tolyandre.visualstudio.com/serilog-throw-context-enricher/_build?definitionId=1) [![Nuget](https://img.shields.io/nuget/v/Serilog.ThrowContext)](https://www.nuget.org/packages/Serilog.ThrowContext)
Captures LogContext on `throw` to enrich exception's log with origin context.

## Use case
Assume an exception is thrown in scope with context properties:
```c#
[HttpGet()]
public WeatherForecast Get()
{
    var weatherForecast = new WeatherForecast
    {
        Date = DateTime.Now.AddDays(1),
        TemperatureC = new Random().Next(-20, 55),
    }

    using (LogContext.PushProperty("WeatherForecast", weatherForecast))
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

This library enriches exception logs with properties from a throwing scope.

It works both with pure Serilog `LogContext.PushProperty()` and framework's logger `_logger.BeginScope()`. Some special cases described below.


## Setup
Add `ThrowContextEnricher` globally or enrich a specific exception handler.

### Global setup
Just add `.Enrich.With<ThrowContextEnricher>()`:

```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    .WriteTo
    ...
    .CreateLogger();
```

### Note on enrichers order
Properties can exist both in exception's origin and exception handler contexts. And they can have different values. In this case order of `ThrowContextEnricher` and `FromLogContext` matters:

<table>
    <tr>
        <td>
<pre lang="c#">
// Logs value from exception origin (A="inner")
Log.Logger = new LoggerConfiguration()
    .Enrich.With&lt;ThrowContextEnricher&gt;()
    .Enrich.FromLogContext()
    ...
</pre>
        </td>
        <td>
<pre lang="c#">
// Logs value from exception handler (A="outer")
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.With&lt;ThrowContextEnricher&gt;()
    ...
</pre>
    </tr>
</table>

```c#
using (LogContext.PushProperty("A", "outer"))
    try
    {
        using (LogContext.PushProperty("A", "inner"))
            throw new Exception();
    }
    catch (Exception ex)
    {
        // A=inner or A=outer?
        Log.Error(ex, "Value of A is {A}");
    }
```



### Local enrichment
To enrich only specific logs, push `ThrowContextEnricher` to LogContext as usual:

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
Note `ThrowContextEnricher.EnsureInitialized()` is used to trigger `ThrowContextEnricher` to begin capturing properties. If this call is omitted, enricher initializes lazily. Thus the first logged exception would miss properties.

## Special cases

### Rethrow
If an exception is rethrown in a different context, the origin property value is not overwritten:
```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    ...
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

### Wrap
If an exception is wrapped into another in a different context, the wrapper context is used. Log of inner exception produces origin value though.

```c#
Log.Logger = new LoggerConfiguration()
    .Enrich.With<ThrowContextEnricher>()
    .Enrich.FromLogContext()
    ...
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
