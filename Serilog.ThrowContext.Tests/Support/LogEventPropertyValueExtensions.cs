using Serilog.Events;

namespace Serilog.ThrowContext.Tests.Support
{
    public static class LogEventPropertyValueExtensions
    {
        public static object LiteralValue(this LogEventPropertyValue @this)
        {
            return ((ScalarValue)@this).Value;
        }
    }
}
