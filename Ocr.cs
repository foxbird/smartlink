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

namespace SmartLink
{
    public class Ocr : IDisposable
    {
        private bool disposedValue;

        private readonly string ALLOWED_CHARACTERS = "abcdefABCDEF1579 \n";

        private readonly static byte MATRIX_THRESHOLD = 200; // 130 calculated
        private readonly static byte SEQUENCE_THRESHOLD = 215;

        private readonly static Rectangle MATRIX_RECTANGLE = new Rectangle(220, 350, 540, 370);
        private readonly static Rectangle SEQUENCE_RECTANGLE = new Rectangle(825, 340, 400, 300);

        private Image Image { get; set; } = null;
        public CaptureResult Result { get; set; } = new CaptureResult();
        private Bitmap Matrix { get; set; } = null;
        private Bitmap Sequences { get; set; } = null;
        private TesseractEngine Tesseract { get; set; } = null;
        private string Id { get; set; } = Guid.NewGuid().ToString("D");

        public Ocr(Stream stream)
        {
            Image = Image.FromStream(stream);
            Image.Save($"procimages/{Id}_original.png", System.Drawing.Imaging.ImageFormat.Png);
            Tesseract = new TesseractEngine(@"./tessdata", "eng", EngineMode.TesseractAndLstm)
            {
                DefaultPageSegMode = PageSegMode.SingleBlock
            };

        }

        public CaptureResult Process()
        {
            if (Image == null)
                throw new InvalidOperationException("Cannot process a null image");

            GetMatrixImage();
            GetSequenceImage();

            Matrix.Save($"procimages/{Id}_matrix.png", System.Drawing.Imaging.ImageFormat.Png);
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
            using Pix pix = BitmapToPix(Matrix);
            using var result = Tesseract.Process(pix);
            var text = result.GetText();
            text = Regex.Replace(text, $"[^{ALLOWED_CHARACTERS}]", "_");

            text = text.Replace("\n\n", "\n");
            text = text.Replace(" ", "\n");
            var cells = text.Split('\n');

            int root = (int)Math.Floor(Math.Sqrt(cells.Length));
            Result.Initialize(root, 0);

            int count = 0;
            foreach (var cell in cells)
            {
                if (String.IsNullOrWhiteSpace(cell))
                    continue;
                int row = (int)Math.Floor((double)(count / root));
                int col = count % root;
                Result.SetMatrixCell(row, col, FindValue(cell));
                count++;
            }
        }

        private void OcrSequences()
        {
            using Pix pix = BitmapToPix(Sequences);
            using var result = Tesseract.Process(pix);
            var text = result.GetText();
            text = Regex.Replace(text, $"[^{ALLOWED_CHARACTERS}]", "_");

            var seqs = text.Split("\n").ToList();
            seqs.RemoveAll(x => String.IsNullOrWhiteSpace(x));
            Result.Initialize(0, seqs.Count);

            int seq = 0;
            int cell = 0;
            foreach (var sequence in seqs)
            {
                if (String.IsNullOrWhiteSpace(sequence))
                    continue;
                var parts = sequence.Split(" ").ToList();
                parts.RemoveAll(x => String.IsNullOrWhiteSpace(x));

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
            var bmp = Image as Bitmap;
            Matrix = bmp.Clone(MATRIX_RECTANGLE, PixelFormat.Format24bppRgb);
            InvertGreyscale(Matrix);
            Threshold(Matrix, MATRIX_THRESHOLD);
        }

        private void GetSequenceImage()
        {
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
