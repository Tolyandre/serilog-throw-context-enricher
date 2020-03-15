using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowContext.Tests.Support;
using System;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class EnrichersOrderTests
    {
        private LogEvent _lastEvent = null;

        [Fact]
        public void LogContextThanThrowContext()
        {
            var log = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.With<ThrowContextEnricher>()
                .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
                .CreateLogger();

            DoLogging(log);

            Assert.Equal("outer", _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public void ThrowContextThanLogContext()
        {
            var log = new LoggerConfiguration()
                .Enrich.With<ThrowContextEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
                .CreateLogger();

            DoLogging(log);

            Assert.Equal("inner", _lastEvent.Properties["A"].LiteralValue());
        }

        private static void DoLogging(ILogger log)
        {
            using (LogContext.Push(new PropertyEnricher("A", "outer")))
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", "inner")))
                        throw new ApplicationException();

                }
                catch (ApplicationException ex)
                {
                    log.Information(ex, "Unit test");
                }
        }
    }
}
