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
            //Example1();
            //Example2();
            Example3();
            //ExampleRethrow();
            //ExampleWrap();

            Log.CloseAndFlush();
        }

        private static void Example1()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<ThrowContextEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

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

        private static void Example2()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<ThrowContextEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

            using (LogContext.PushProperty("A", "outer"))
                try
                {
                    using (LogContext.PushProperty("A", "inner"))
                    {
                        throw new Exception();
                    }

                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Value of A is {A}");
                }
        }

        private static void Example3()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

            ThrowContextEnricher.EnsureInitialized();

            using (LogContext.PushProperty("A", "outer"))
                try
                {
                    using (LogContext.PushProperty("A", "inner"))
                    {
                        throw new Exception();
                    }

                }
                catch (Exception ex)
                {
                    using (LogContext.Push(new ThrowContextEnricher()))
                    {
                        Log.Error(ex, "Value of A is {A}");
                    }
                }
        }

        private static void ExampleRethrow()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<ThrowContextEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

            try
            {
                try
                {
                    using (LogContext.PushProperty("A", 1))
                        throw new ApplicationException();
                }
                catch
                {
                    using (LogContext.PushProperty("A", 2))
                    using (LogContext.PushProperty("B", 2))
                        throw;
                }
            }
            catch (ApplicationException ex)
            {
                Log.Information(ex, "A={A}, B={B}");
            }
        }

        public static void ExampleWrap()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.With<ThrowContextEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .CreateLogger();

            try
            {
                try
                {
                    using (LogContext.PushProperty("A", 1))
                        throw new Exception();
                }
                catch (Exception ex)
                {
                    using (LogContext.PushProperty("A", 2))
                    using (LogContext.PushProperty("B", 2))
                        throw new ApplicationException("Wrapper", ex);
                }
            }
            catch (ApplicationException ex)
            {
                Log.Information(ex, "A={A}, B={B}");
                Log.Information(ex.InnerException, "A={A}, B={B}");
            }
        }
    }
}
