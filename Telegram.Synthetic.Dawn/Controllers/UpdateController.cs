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

        [HttpGet("hello")]
        public string Hello() => "world";

        [HttpPost("update")]
        public async Task<IActionResult> Update(Update update)
        {
            await _botService.HandleUpdateAsync(update);
            return Ok();
        }
    }
}