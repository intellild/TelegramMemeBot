using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Synthetic.Dawn.Services
{
    public class BotService
    {
        private readonly ITelegramBotClient _botClient;
        private User? _me = null;

        public BotService(IConfiguration configuration)
        {
            _botClient = new TelegramBotClient(configuration["BOT_TOKEN"]);
        }

        public void StartPolling(IUpdateHandler handler, CancellationToken cancellationToken)
        {
            _botClient.StartReceiving(handler, cancellationToken);
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Message != null)
            {
                await this.HandleMessage(update.Message);
            }
        }

        private async Task HandleMessage(Message message)
        {
            if (message.Entities == null || message.Entities.Length == 0)
            {
                return;
            }

            var firstEntity = message.Entities[0];
            if (firstEntity.Type != MessageEntityType.Mention || (firstEntity.User.IsBot) ||
                firstEntity.User.Id != _me?.Id)
            {
                return;
            }
            
            
        }

        public async Task Initialize()
        {
            this._me = await _botClient.GetMeAsync();
        }
    }
}