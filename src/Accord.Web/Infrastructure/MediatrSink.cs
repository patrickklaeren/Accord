using System;
using Accord.Bot.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Accord.Web.Infrastructure
{
    public class MediatrSink : ILogEventSink
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public MediatrSink(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < LogEventLevel.Warning)
                return;

            using var scope = _serviceScopeFactory.CreateScope();
            var mediatr = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediatr.Publish(new LogNotification(logEvent));
        }
    }
}
