using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace Accord.Web.Infrastructure
{
    public static class LoggerSinkConfigurationExtensions
    {
        public static LoggerConfiguration Mediatr(this LoggerSinkConfiguration loggerConfiguration, IServiceScopeFactory serviceScopeFactory)
        {
            return loggerConfiguration.Sink(new MediatrSink(serviceScopeFactory));
        }
    }
}