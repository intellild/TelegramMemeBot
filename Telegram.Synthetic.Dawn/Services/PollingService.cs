using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Telegram.Synthetic.Dawn.Services
{
    public class PollingService : IHostedService
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly IWebHostEnvironment _environment;
        private readonly BotService _botService;

        public PollingService(IWebHostEnvironment environment, BotService botService)
        {
            _environment = environment;
            _botService = botService;
        }

        private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            await _botService.HandleUpdateAsync(update);
        }

        private async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
        }


#pragma warning disable 1998
        public async Task StartAsync(CancellationToken cancellationToken)
#pragma warning restore 1998
        {
            await _botService.Initialize();
            if (_environment.IsDevelopment() || Environment.GetEnvironmentVariable("POLLING") != null)
            {
                _botService.StartPolling(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                    _cancellationTokenSource.Token);
            }
        }

#pragma warning disable 1998
        public async Task StopAsync(CancellationToken cancellationToken)
#pragma warning restore 1998
        {
            _cancellationTokenSource.Cancel();
        }
    }
}