using SmartLink.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Tesseract;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SmartLink
{
    public class Ocr : IDisposable
    {
        private bool disposedValue;

        private static readonly string ALLOWED_CHARACTERS = "abcdefABCDEF1579 \n";

        private static readonly byte MATRIX_THRESHOLD = 210; // 130 calculated
        private static readonly byte SEQUENCE_THRESHOLD = 215;

        private static readonly Rectangle MATRIX_RECTANGLE = new Rectangle(220, 350, 540, 370);
        private static readonly Rectangle SEQUENCE_RECTANGLE = new Rectangle(825, 340, 400, 300);

        private Image Image { get; set; } = null;
        public CaptureResult Result { get; set; } = new CaptureResult();
        private Bitmap Matrix { get; set; } = null;
        private Bitmap Sequences { get; set; } = null;
        private static TesseractEngine Tesseract { get; set; } = null;
        private string Id { get; } = Guid.NewGuid().ToString("D");

        private readonly ILoggerFactory _logFactory;
        private readonly ILogger _logger;

        private static void Initialize(ILogger _logger)
        {
            _logger.LogInformation("Initializing static Tesseract");
            Tesseract = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractAndLstm)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };

            Tesseract.SetVariable("tessedit_char_whitelist", ALLOWED_CHARACTERS);
            Tesseract.SetVariable("user_patterns_file", @"./tessdata/cyber_patterns");
            // \n (char or digit), \c (char), \d (digit), \p (punct), \a (lower), \A (upper), \* any number (\A\d and \d\A)
            Tesseract.SetVariable("user_words_file", @"./tessdata/cyber_words"); // BD, 1C, E9, 55, 7A, 1F
            Tesseract.SetVariable("load_system_dawg", false); // Don't load sys dictionary
            Tesseract.SetVariable("load_freq_dawg", false); // Don't load word freeuence
        }

        public Ocr(Stream stream, ILoggerFactory loggerFactory)
        {
            _logFactory = loggerFactory;
            _logger = _logFactory.CreateLogger<Ocr>();

            if (Tesseract == null)
                Initialize(_logger);

            _logger.LogInformation($"Saving original stream with id {Id}");
            Image = Image.FromStream(stream);
            Image.Save($"procimages/{Id}_original.png", System.Drawing.Imaging.ImageFormat.Png);
        }

        public CaptureResult Process()
        {
            if (Image == null)
                throw new InvalidOperationException("Cannot process a null image");

            GetMatrixImage();
            GetSequenceImage();

            _logger.LogInformation($"Saving matrix image result file for id {Id}");
            Matrix.Save($"procimages/{Id}_matrix.png", System.Drawing.Imaging.ImageFormat.Png);

            _logger.LogInformation($"Saving sequence image result file for id {Id}");
            Sequences.Save($"procimages/{Id}_sequences.png", System.Drawing.Imaging.ImageFormat.Png);

            OcrMatrix();
            OcrSequences();

            return Result;
        }

        private static string FindValue(string value)
        {
            var cellText = value.Trim();
            cellText = cellText.ToUpper();

            // Match on the second character, more precisely
            if (cellText.Length >= 2)
            {
                switch (cellText[1])
                {
                    case 'D':
                        cellText = "BD";
                        break;
                    case 'C':
                        cellText = "1C";
                        break;
                    case '9':
                        cellText = "E9";
                        break;
                    case '5':
                        cellText = "55";
                        break;
                    case 'A':
                        cellText = "7A";
                        break;
                    case 'F':
                        cellText = "FF";
                        break;
                    default:
                        break;
                }
            }

            // Match on first or second
            if (cellText.Length >= 1)
            {
                switch (cellText[0])
                {
                    case 'B':
                    case 'D':
                        cellText = "BD";
                        break;
                    case '1':
                    case 'C':
                        cellText = "1C";
                        break;
                    case 'E':
                    case '9':
                        cellText = "E9";
                        break;
                    case '5':
                        cellText = "55";
                        break;
                    case '7':
                    case 'A':
                        cellText = "7A";
                        break;
                    case 'F':
                        cellText = "FF";
                        break;
                    default:
                        break;
                }
            }

            return cellText;
        }

        private void OcrMatrix()
        {
            _logger.LogInformation("Performing OCR on matrix image");

            using Pix pix = BitmapToPix(Matrix);

            _logger.LogInformation("Calling Tesseract on matrix image");
            using var result = Tesseract.Process(pix);
            var text = result.GetText();

            _logger.LogInformation($"Tesseract gave result: {text.Replace("\n", "\\n")}");

            _logger.LogInformation($"Saving resulting tesseract process for matrix id {Id}");
            using (var stream = File.CreateText($"procimages/{Id}_matrix.txt"))
            {
                stream.Write(text);
            }

            var rows = text.Split("\n");

            // Remove any empty rows (like the last one)
            rows = rows.ToList().Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();

            _logger.LogInformation($"Initializing matrix with size {rows.Length}");
            Result.Initialize(rows.Length);

            for (int rowPos = 0; rowPos < rows.Length; rowPos++)
            {
                var row = rows[rowPos];
                var cols = row.Split(" ");

                if (cols.Length > rows.Length)
                {
                    _logger.LogWarning($"Row {rowPos} has too many cells");
                }

                for (int colPos = 0; colPos < cols.Length; colPos++)
                {
                    var cell = cols[colPos];
                    // Can't create an unsquare matrix

                    if (colPos >= rows.Length)
                        continue;

                    if (String.IsNullOrWhiteSpace(cell))
                    {
                        _logger.LogWarning($"Tesseract OCR failed for cell ({rowPos}, {colPos})");
                        cell = "__";
                    }

                    Result.SetMatrixCell(rowPos, colPos, FindValue(cell));
                }
            }
        }

        private void OcrSequences()
        {
            _logger.LogInformation("Performing OCR on sequence image");
            using Pix pix = BitmapToPix(Sequences);

            _logger.LogInformation("Calling Tesseract on sequence image");
            using var result = Tesseract.Process(pix);
            var text = result.GetText();

            _logger.LogInformation($"Tesseract gave result: {text.Replace("\n", "\\n")}");

            _logger.LogInformation($"Saving resulting tesseract process for sequence id {Id}");
            using (var stream = File.CreateText($"procimages/{Id}_sequence.txt"))
            {
                stream.Write(text);
            }

            var seqs = text.Split("\n").ToList();
            seqs.RemoveAll(x => String.IsNullOrWhiteSpace(x));

            _logger.LogInformation($"Initializing sequences with count {seqs.Count}");
            Result.Initialize(0, seqs.Count);

            int seq = 0;
            int cell = 0;
            foreach (var sequence in seqs)
            {
                if (String.IsNullOrWhiteSpace(sequence))
                    continue;
                var parts = sequence.Split(" ").ToList();
                parts.RemoveAll(x => String.IsNullOrWhiteSpace(x));

                _logger.LogInformation($"Initializing sequence {seq} with {parts.Count} cells");
                Result.Sequences[seq].Initialize(parts.Count);

                foreach(var part in parts)
                {
                    Result.SetSequenceCell(seq, cell, FindValue(part));
                    cell++;
                }

                cell = 0;
                seq++;
            }

        }

        private void GetMatrixImage()
        {
            _logger.LogInformation($"Getting matrix image from full scale");
            var bmp = Image as Bitmap;
            Matrix = bmp.Clone(MATRIX_RECTANGLE, PixelFormat.Format24bppRgb);
            InvertGreyscale(Matrix);
            Threshold(Matrix, MATRIX_THRESHOLD);
        }

        private void GetSequenceImage()
        {
            _logger.LogInformation($"Getting sequence image from full scale");
            var bmp = Image as Bitmap;
            Sequences = bmp.Clone(SEQUENCE_RECTANGLE, PixelFormat.Format24bppRgb);
            InvertGreyscale(Sequences);
            Threshold(Sequences, SEQUENCE_THRESHOLD);
        }

        private static Pix BitmapToPix(Bitmap bitmap)
        {
            // Put the bitmap into a memory stream for Pix to load
            using MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return Pix.LoadFromMemory(stream.ToArray());
        }

        private static unsafe void Threshold(Bitmap bitmap, byte threshold)
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte bpp = 24;
            byte* scan0 = (byte*)bmpData.Scan0.ToPointer();

            // Apply the threshold to all the bits
            for (int row = 0; row < bmpData.Height; row++)
            {
                for (int col = 0; col < bmpData.Width; col++)
                {
                    byte* data = scan0 + row * bmpData.Stride + col * bpp / 8;
                    byte blue = data[0];
                    byte green = data[1];
                    byte red = data[2];

                    // Calculate grayscale and invert
                    byte pixel = (byte)(red > threshold ? 255 : 0);
                    blue = green = red = pixel;

                    data[0] = blue;
                    data[1] = green;
                    data[2] = red;
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        private static unsafe void InvertGreyscale(Bitmap bitmap)
        {
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            byte bpp = 24;
            byte* scan0 = (byte*)bmpData.Scan0.ToPointer();

            for (int row = 0; row < bmpData.Height; row++)
            {
                for (int col = 0; col < bmpData.Width; col++)
                {
                    byte* data = scan0 + row * bmpData.Stride + col * bpp / 8;
                    byte blue = data[0];
                    byte green = data[1];
                    byte red = data[2];

                    // Calculate grayscale and invert
                    byte avg = (byte)(0.2989 * red + 0.5870 * green + 0.1140 * blue);
                    blue = green = red = (byte)(255 - avg);

                    data[0] = blue;
                    data[1] = green;
                    data[2] = red;
                }
            }

            bitmap.UnlockBits(bmpData);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Image != null)
                        Image.Dispose();
                    if (Matrix != null)
                        Matrix.Dispose();
                    if (Sequences != null)
                        Sequences.Dispose();
                    if (Tesseract != null)
                        Tesseract.Dispose();

                    Image = null;
                    Matrix = null;
                    Sequences = null;
                    Tesseract = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
