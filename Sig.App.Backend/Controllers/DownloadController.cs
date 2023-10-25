using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Sig.App.Backend.Services.Crypto;
using Sig.App.Backend.Services.Files;

namespace Sig.App.Backend.Controllers
{
    [Route("[controller]")]
    public class DownloadController : Controller
    {
        [Route("file/{container}/{*fileId}")]
        public async Task<IActionResult> File(
            [FromServices] ISignatureService signature,
            [FromServices] IFileManager fileManager,
            string container,
            string fileId)
        {
            if (!signature.VerifySignedUrl(Request.GetEncodedPathAndQuery())) return Unauthorized();

            var file = await fileManager.DownloadFile(container, fileId);
            if (file == null) return NotFound();

            return File(file.Content, file.ContentType, file.FileName);
        }
    }
}