using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowContext.Tests.Support;
using System;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class PreventStackOverflowTests
    {
        private LogEvent _lastEvent = null;

        [Fact]
        public void ThrowInEnricherScope()
        {
            var log = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
             .CreateLogger();

            using (LogContext.Push(new ThrowContextEnricher()))
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new ApplicationException();

                }
                catch (ApplicationException ex)
                {
                    log.Information(ex, "Unit test");
                }
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }
    }
}
