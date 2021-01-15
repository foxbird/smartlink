using Microsoft.AspNetCore.Mvc;
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
        [HttpGet]
        public async Task<ActionResult<CaptureResult>> Get()
        {
            try
            {
                // Capture from the card
                Capture capture = new Capture();
                MemoryStream stream = new MemoryStream();
                await capture.CapturePhoto(stream);
                stream.Position = 0;
                
                // For debugging
                //using FileStream stream = System.IO.File.OpenRead(@"procimages/618f82be-b899-47dd-a7a7-966484772220_original.png");

                // OCR and return
                using Ocr ocr = new Ocr(stream);
                ocr.Process();
                return Ok(ocr.Result);
            } 
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message + "\n" + e.StackTrace });
            }
        }
    }
}
