using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.ThrowContext.Tests.Support;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class FrameworkLoggerTests
    {
        private LogEvent _lastEvent = null;
        private readonly Microsoft.Extensions.Logging.ILogger _frameworkLogger;
        private readonly Serilog.ILogger _serilogLogger;

        public FrameworkLoggerTests()
        {
            ThrowContextEnricher.EnsureInitialized();

            _serilogLogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.With<ThrowContextEnricher>()
                    .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
                    .CreateLogger();

            _frameworkLogger = new SerilogLoggerProvider(_serilogLogger)
                .CreateLogger(nameof(FrameworkLoggerTests));
        }

        [Fact]
        public void BeginScopeConfiguredCorrectly()
        {
            using (_frameworkLogger.BeginScope(new Dictionary<string, object> { { "A", 1 } }))
                _frameworkLogger.Log(Microsoft.Extensions.Logging.LogLevel.Information, 0, new object(), null, null);

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public void CapturesContextProperty()
        {
            try
            {
                using (_frameworkLogger.BeginScope(new Dictionary<string, object> { { "A", 1 } }))
                    throw new ApplicationException();
            }
            catch (ApplicationException ex)
            {
                using (Serilog.Context.LogContext.Push(new ThrowContextEnricher()))
                    _serilogLogger.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public async Task CapturesContextPropertyAsync()
        {
            try
            {
                await Task.Delay(1);

                using (_frameworkLogger.BeginScope(new Dictionary<string, object> { { "A", 1 } }))
                    throw new ApplicationException();
            }
            catch (ApplicationException ex)
            {
                await Task.Delay(1);

                using (Serilog.Context.LogContext.Push(new ThrowContextEnricher()))
                    _serilogLogger.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public void CapturesContextPropertyWithNestedScopes()
        {
            try
            {
                using (_frameworkLogger.BeginScope(new Dictionary<string, object> { { "A", 1 } }))
                using (_frameworkLogger.BeginScope(new Dictionary<string, object> { { "B", 2 } }))
                    throw new ApplicationException();
            }
            catch (ApplicationException ex)
            {
                using (Serilog.Context.LogContext.Push(new ThrowContextEnricher()))
                    _serilogLogger.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
            Assert.Equal(2, _lastEvent.Properties["B"].LiteralValue());
        }
    }
}
