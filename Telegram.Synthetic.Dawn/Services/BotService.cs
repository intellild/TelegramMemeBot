using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Telegram.Synthetic.Dawn.Services
{
    public class BotService
    {
        private readonly IConfiguration _configuration;
        private readonly ITelegramBotClient _botClient;

        public BotService(IConfiguration configuration)
        {
            _configuration = configuration;
            _botClient = new TelegramBotClient(_configuration["BOT_TOKEN"]);
        }

        public void StartPolling(IUpdateHandler handler, CancellationToken cancellationToken)
        {
            _botClient.StartReceiving(handler, cancellationToken);
        }

        public async Task HandleUpdateAsync(Update update)
        {
        }
    }
}