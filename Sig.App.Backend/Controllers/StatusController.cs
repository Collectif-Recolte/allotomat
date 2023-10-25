using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        public async Task<IActionResult> Get(
            [FromServices] AppDbContext dbContext)
        {
            var db = await CheckDatabaseStatus(dbContext);
            // Add more checks here as needed
            
            var ready = db; // Ready should be true if all checks are successful
            
#if NCRUNCH
            var version = "ncrunch";
#else
            var version = $"{GitVersionInformation.CommitDate}/{GitVersionInformation.Sha}";
#endif

            return new ObjectResult(new {db, ready, version}) {
                StatusCode = ready
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable
            };
        }

        private static async Task<bool> CheckDatabaseStatus(AppDbContext dbContext)
        {
            try
            {
                // Execute arbitrary database query
                await dbContext.Users.AnyAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}