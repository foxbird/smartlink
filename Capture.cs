using Microsoft.Extensions.Logging;
using SmartLink.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace SmartLink
{
    public class Capture
    {
        public string DeviceId { get; set; }
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;
        private readonly ILoggerFactory _logFactory;

        public Capture(ILoggerFactory logFactory)
        {
            _logFactory = logFactory;
            _logger = _logFactory.CreateLogger<Capture>();
        }

        public Capture(string deviceId)
        {
            DeviceId = deviceId;
        }

        public async Task CapturePhoto(Stream stream, uint width = 1920, uint height = 1080)
        {
            // Capture a semaphore so we don't use the card twice
            _logger.LogInformation($"Waiting on camera semaphore to become available");
            await Semaphore.WaitAsync();
            _logger.LogInformation($"Captured camera semaphore");
            MediaCaptureInitializationSettings mcis = new MediaCaptureInitializationSettings();
            if (!String.IsNullOrEmpty(DeviceId))
            {
                mcis.VideoDeviceId = DeviceId;
            }


            MediaCapture mediaCapture = null;
            try
            {
                _logger.LogInformation($"Initializing media capture for bitmap capturing ({width}x{height})");
                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(mcis);
                ImageEncodingProperties iep = ImageEncodingProperties.CreateBmp();
                iep.Width = width;
                iep.Height = height;

                using (InMemoryRandomAccessStream imras = new InMemoryRandomAccessStream())
                {
                    _logger.LogInformation($"Capturing photo to random access stream");
                    await mediaCapture.CapturePhotoToStreamAsync(iep, imras);

                    _logger.LogInformation($"Copying RAS to IO Stream");
                    var imrasNative = imras.AsStream();
                    imrasNative.Position = 0;
                    await imrasNative.CopyToAsync(stream);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (mediaCapture != null)
                    mediaCapture.Dispose();
                Semaphore.Release();
            }
        }

        public async Task<List<Device>> EnumerateDevices()
        {
            List<Device> deviceResults = new List<Device>();
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                var d = new Device() { Name = device.Name, Id = device.Id };
                deviceResults.Add(d);
            }

            return deviceResults;
        }
    }
}
