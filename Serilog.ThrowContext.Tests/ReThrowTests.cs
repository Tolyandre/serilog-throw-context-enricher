using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.ThrowContext.Tests.Support;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Serilog.ThrowContext.Tests
{
    public class ReThrowTests
    {
        private LogEvent _lastEvent = null;
        private readonly ILogger _log;

        public ReThrowTests()
        {
            ThrowContextEnricher.EnsureInitialized();

            _log = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Sink(new DelegatingSink(e => _lastEvent = e))
              .CreateLogger();
        }

        [Fact]
        public void ReThrowCapturesProperty()
        {
            try
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new ApplicationException();
                }
                catch
                {
                    using (LogContext.Push(new PropertyEnricher("B", 2)))
                        throw;
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
        public async Task ReThrowCapturesPropertyAsync()
        {
            try
            {
                try
                {
                    await Task.Delay(1);

                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new ApplicationException();
                }
                catch
                {
                    await Task.Delay(1);

                    using (LogContext.Push(new PropertyEnricher("B", 2)))
                        throw;
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReThrowDoesNotOverrideOriginalProperty(bool preserveStack)
        {
            try
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new ApplicationException();
                }
                catch (Exception ex)
                {
                    using (LogContext.Push(new PropertyEnricher("A", 2)))
                        if (preserveStack)
                            throw;
                        else
                            throw ex;
                }
            }
            catch (ApplicationException ex)
            {
                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.Equal(1, _lastEvent.Properties["A"].LiteralValue());
        }

        [Fact]
        public void ReThrowDoesNotCaptureOriginalPropertyForNewException()
        {
            try
            {
                try
                {
                    using (LogContext.Push(new PropertyEnricher("A", 1)))
                        throw new FormatException();
                }
                catch
                {
                    using (LogContext.Push(new PropertyEnricher("B", 2)))
                        throw new ApplicationException();
                }
            }
            catch (ApplicationException ex)
            {
                using (LogContext.Push(new ThrowContextEnricher()))
                    _log.Information(ex, "Unit test");
            }

            Assert.False(_lastEvent.Properties.ContainsKey("A"));
            Assert.Equal(2, _lastEvent.Properties["B"].LiteralValue());
        }
    }
}
