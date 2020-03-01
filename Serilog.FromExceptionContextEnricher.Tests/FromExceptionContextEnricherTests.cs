using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowingContext.Tests.Support;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.ThrowingContext.Tests
{
    public class ThrowingContextEnricherTests
    {

        private LogEvent _lastEvent;
        private readonly ILogger _log;

        public ThrowingContextEnricherTests()
        {
            ThrowingContextEnricher.EnsureInitialized();

            _lastEvent = null;

            _log = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
              .CreateLogger();
        }

        [Fact]
        public void CapturesContextProperty()
        {
            try
            {
                using (LogContext.Push(new PropertyEnricher("A", 1)))
                    throw new ApplicationException();

            }
            catch (ApplicationException ex)
            {
                using (LogContext.Push(new ThrowingContextEnricher()))
                {
                    _log.Information(ex, "Unit test");
                }
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public async Task CapturesContextPropertyAsync()
        {
            try
            {
                await Task.Delay(1);

                using (LogContext.Push(new PropertyEnricher("A", 1)))
                    throw new ApplicationException();
            }
            catch (ApplicationException ex)
            {
                await Task.Delay(1);

                using (LogContext.Push(new ThrowingContextEnricher()))
                {
                    _log.Information(ex, "Unit test");
                }
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }
    }
}
