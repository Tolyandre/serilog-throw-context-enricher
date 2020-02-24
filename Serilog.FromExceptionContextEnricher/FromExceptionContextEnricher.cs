using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Serilog.FromExceptionContextEnricher
{
    public class FromExceptionContextEnricher : ILogEventEnricher
    {
        static readonly ConditionalWeakTable<Exception, List<ILogEventEnricher>> ConditionalWeakTable =
            new ConditionalWeakTable<Exception, List<ILogEventEnricher>>();

        static FromExceptionContextEnricher()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            var exceptionContexts = ConditionalWeakTable.GetValue(e.Exception, _ => new List<ILogEventEnricher>());
            exceptionContexts.Add(LogContext.Clone());
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            Exception exception = logEvent.Exception;

            while (exception != null)
            {
                if (ConditionalWeakTable.TryGetValue(exception, out List<ILogEventEnricher> contexts))
                {
                    foreach (var context in contexts.AsEnumerable().Reverse())
                    {
                        context.Enrich(logEvent, propertyFactory);
                    }
                }

                exception = exception.InnerException;
            }
        }
    }
}
