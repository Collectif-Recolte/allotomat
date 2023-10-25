using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Sig.App.Backend.Controllers
{
    [ApiController]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> logger;

        public LogController(ILogger<LogController> logger)
        {
            this.logger = logger;
        }

        [HttpPost("log")]
        public IActionResult LogBatch(LogMessage[] messages)
        {
            foreach (var message in messages)
            {
                logger.Log(message.Level, "[FE] " + message.Message);
            }

            return NoContent();
        }

        public class LogMessage
        {
            public LogLevel Level { get; set; }
            public string Message { get; set; }
        }
    }
}