using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Synthetic.Dawn.Services;

namespace Telegram.Synthetic.Dawn.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly BotService _botService;

        public UpdateController(BotService botService)
        {
            _botService = botService;
        }

        public async Task<ActionResult<string>> Update(Update update)
        {
            await _botService.HandleUpdateAsync(update);
            return Ok("hello");
        }
    }
}