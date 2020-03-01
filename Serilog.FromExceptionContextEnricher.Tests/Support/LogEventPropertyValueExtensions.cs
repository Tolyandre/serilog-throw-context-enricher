using Serilog.Events;

namespace Serilog.ThrowingContext.Tests.Support
{
    public static class LogEventPropertyValueExtensions
    {
        public static object LiteralValue(this LogEventPropertyValue @this)
        {
            return ((ScalarValue)@this).Value;
        }
    }
}
