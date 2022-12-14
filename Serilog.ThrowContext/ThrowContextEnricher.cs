using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Serilog.ThrowContext
{
    public class ThrowContextEnricher : ILogEventEnricher
    {
        static readonly ConditionalWeakTable<Exception, List<(ILogEventEnricher EnricherContext, ExecutionContext ExecutionContext)>> ConditionalWeakTable =
            new ConditionalWeakTable<Exception, List<(ILogEventEnricher EnricherContext, ExecutionContext ExecutionContext)>>();

        static readonly ConditionalWeakTable<LogEvent, object> EnrichedLogEvents = new ConditionalWeakTable<LogEvent, object>();

        static ThrowContextEnricher()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        /// <summary>
        /// Ensures that capturing of thrown exception log context is initialized.
        ///
        /// This method should be invoked only if <c>ThrowContextEnricher</c> is not referenced before the actual exception may occur.
        /// </summary>
        public static void EnsureInitialized()
        {
            // just triggers static ctor
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var exceptionContexts = ConditionalWeakTable.GetOrCreateValue(e.Exception);
            exceptionContexts.Add((LogContext.Clone(), ExecutionContext.Capture()));
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Exception == null)
                return;

            // prevent recursion if an exception is thrown inside our enricher scope
            bool alreadyEnriched = true;
            EnrichedLogEvents.GetValue(logEvent, _ =>
            {
                alreadyEnriched = false;
                return null;
            });

            if (alreadyEnriched)
                return;

            EnrichInternal(logEvent, propertyFactory);
        }

        private static void EnrichInternal(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            Exception exception = logEvent.Exception;

            while (exception != null)
            {
                if (ConditionalWeakTable.TryGetValue(exception, out List<(ILogEventEnricher EnricherContext, ExecutionContext ExecutionContext)> contexts))
                {
                    foreach (var context in contexts)
                    {
                        // ExecutionContext is only needed to support framework's logger BeginScope (Serilog.Extensions.Logging)
                        ExecutionContext.Run(context.ExecutionContext, _ =>
                        {
                            context.EnricherContext.Enrich(logEvent, propertyFactory);
                        }, null);
                    }
                }

                exception = exception.InnerException;
            }
        }
    }
}
