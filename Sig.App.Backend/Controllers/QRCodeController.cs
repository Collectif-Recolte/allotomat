using Microsoft.AspNetCore.Mvc;
using Sig.App.Backend.Services.QRCode;
using System.ComponentModel.DataAnnotations;

namespace Sig.App.Backend.Controllers
{
    [Route("qr-code")]
    public class QRCodeController : Controller
    {
        private readonly IQRCodeService qrCodeService;

        public QRCodeController(IQRCodeService qrCodeService)
        {
            this.qrCodeService = qrCodeService;
        }

        [HttpPost("decrypt")]
        public IActionResult Decrypt([FromBody] DecryptRequest request)
        {
            return Ok(qrCodeService.ReadQRCode(request.QRCode));
        }

        public class DecryptRequest
        {
            [Required]
            public string QRCode { get; set; }
        }
    }
}
