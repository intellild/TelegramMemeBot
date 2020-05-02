using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Telegram.Synthetic.Dawn.Services
{
    public class BotService : IHostedService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _botClient;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public BotService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
            _botClient = new TelegramBotClient(_configuration["BOT_TOKEN"]);
        }

        private void StartPolling()
        {
            _botClient.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync),
                _cancellationTokenSource.Token);
        }

        public async Task HandleUpdateAsync(Update update)
        {
        }

        private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
        {
            await HandleUpdateAsync(update);
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_environment.IsDevelopment())
            {
                var me = await _botClient.GetMeAsync();
                Console.WriteLine($"{me.Id}");
                StartPolling();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource.Cancel();
        }
    }
}