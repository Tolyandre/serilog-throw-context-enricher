using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using Serilog.ThrowContext;
using System;

namespace ExampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .Enrich.With<ThrowContextEnricher>()

             .WriteTo.Console(new RenderedCompactJsonFormatter())
             .CreateLogger();

            Example1();

            Log.CloseAndFlush();
        }

        private static void Example1()
        {
            using (LogContext.PushProperty("MyName", "Example1"))
                try
                {
                    using (LogContext.PushProperty("MyName", "Inside TRY in Example1"))
                    using (LogContext.PushProperty("Local", "Value from origin context"))
                    {
                        throw new InvalidOperationException("test");
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception caught in MakeLogs()");
                }
        }
    }
}
