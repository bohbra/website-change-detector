using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace WebsiteChangeDetector.Console.Logging
{
    internal class ClassNameEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // skip if no source context
            if (!logEvent.Properties.TryGetValue("SourceContext", out var sourceContextProp))
                return;

            // serilog provides quoted string so remove the quotes for
            // instance "abc.lmn.xyz"
            var fullContext = sourceContextProp.ToString().Replace("\"", string.Empty);

            // add class name property
            var className = fullContext.Split('.').LastOrDefault();
            if (!string.IsNullOrEmpty(className))
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ClassName", className));
        }
    }
}
