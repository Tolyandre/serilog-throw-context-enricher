using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowContext.Tests.Support;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class CaptureOriginContextTests
    {
        private LogEvent _lastEvent = null;
        private readonly ILogger _log;

        public CaptureOriginContextTests()
        {
            ThrowContextEnricher.EnsureInitialized();

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
                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");

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

                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }
    }
}
