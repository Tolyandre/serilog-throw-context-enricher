using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace AspNetCoreSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public RedirectResult Get()
        {
            return Redirect("WeatherForecast/get1");
        }

        [HttpGet("get1")]
        public WeatherForecast Get1()
        {
            var rng = new Random();
            using (LogContext.PushProperty("WeatherForecast", new
            {
                Date = DateTime.Now.AddDays(1),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }))
            {
                _logger.LogInformation("Today Weather forecast 1");

                throw new InvalidOperationException("See context properties in log file");
            }
        }

        [HttpGet("get2")]
        public WeatherForecast Get2()
        {
            var rng = new Random();
            using (_logger.BeginScope(new
            {
                Date = DateTime.Now.AddDays(1),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }))
            {
                _logger.LogInformation("Today Weather forecast 2");

                throw new InvalidOperationException("See context properties in log file");
            }
        }
    }
}
