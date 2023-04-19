using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Context;

// Demo of configuration from appsettings.json file

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    using (LogContext.PushProperty("PropertyMustBeInLog", "1"))
        throw new ApplicationException("Hello, world!");

}
catch (ApplicationException ex)
{
    logger.Information(ex, "Test exception");
}