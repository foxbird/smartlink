using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartLink.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace SmartLink.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaptureController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _logFactory;

        public CaptureController(ILogger<CaptureController> logger, ILoggerFactory factory)
        {
            _logger = logger;
            _logFactory = factory;
        }

        [HttpGet]
        public async Task<ActionResult<CaptureResult>> Get()
        {
            _logger.LogInformation("Client requested capture");
            try
            {
                // Capture from the card
                Capture capture = new Capture(_logFactory);
                MemoryStream stream = new MemoryStream();
                await capture.CapturePhoto(stream);
                stream.Position = 0;

                // For debugging
                //using FileStream stream = System.IO.File.OpenRead(@"procimages/618f82be-b899-47dd-a7a7-966484772220_original.png");

                // OCR and return
                using Ocr ocr = new Ocr(stream, _logFactory);
                ocr.Process();
                return Ok(ocr.Result);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception during capture", e);
                return BadRequest(new { message = e.Message + "\n" + e.StackTrace });
            }
        }
    }
}
