using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowContext.Tests.Support;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class WrapTests
    {
        private LogEvent _lastEvent = null;
        private readonly ILogger _log;

        public WrapTests()
        {
            ThrowContextEnricher.EnsureInitialized();

            _log = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
              .CreateLogger();
        }

        [Fact]
        public void WrapIncludesOriginalContext()
        {
            try
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new FormatException();
                }
                catch(Exception ex)
                {
                    using (LogContext.Push(new PropertyEnricher("B", 2)))
                        throw new ApplicationException("Wrapper", ex);
                }
            }
            catch (ApplicationException ex)
            {
                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
            Assert.Equal(2, _lastEvent.Properties["B"].LiteralValue());
        }

        [Fact]
        public async Task WrapIncludesOriginalContextAsync()
        {
            try
            {
                try
                {
                    await Task.Delay(1);

                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new FormatException();
                }
                catch (Exception ex)
                {
                    await Task.Delay(1);

                    using (LogContext.Push(new PropertyEnricher("B", 2)))
                        throw new ApplicationException("Wrapper", ex);
                }
            }
            catch (ApplicationException ex)
            {
                await Task.Delay(1);

                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
            Assert.Equal(2, _lastEvent.Properties["B"].LiteralValue());
        }

        [Fact]
        public void WrapOverridesOriginalProperty()
        {
            try
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new FormatException();
                }
                catch (Exception ex)
                {
                    using (LogContext.Push(new PropertyEnricher("A", 2)))
                        throw new ApplicationException("Wrapper", ex);
                }
            }
            catch (ApplicationException ex)
            {
                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.Equal(2, _lastEvent.Properties["A"].LiteralValue());
        }
    }
}
