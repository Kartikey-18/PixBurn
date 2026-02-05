using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using PixBurn.Models;
using PixBurn.Services.Interfaces;

namespace PixBurn.Services;

public class DicomReaderService : IDicomReader
{
    public DicomFileItem ReadFileInfo(string filePath)
    {
        var file = DicomFile.Open(filePath);
        var dataset = file.Dataset;

        int width = dataset.GetSingleValueOrDefault(DicomTag.Columns, 0);
        int height = dataset.GetSingleValueOrDefault(DicomTag.Rows, 0);
        int bitsAllocated = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 8);
        int samplesPerPixel = dataset.GetSingleValueOrDefault(DicomTag.SamplesPerPixel, 1);
        string photometric = dataset.GetSingleValueOrDefault(DicomTag.PhotometricInterpretation, "MONOCHROME2");

        return new DicomFileItem
        {
            FilePath = filePath,
            FileSizeBytes = new FileInfo(filePath).Length,
            Width = width,
            Height = height,
            BitsAllocated = bitsAllocated,
            SamplesPerPixel = samplesPerPixel,
            PhotometricInterpretation = photometric,
            Thumbnail = CreateThumbnail(file, 80)
        };
    }

    public BitmapSource LoadFullImage(string filePath)
    {
        var file = DicomFile.Open(filePath);
        return RenderDicomToBitmap(file);
    }

    public (byte[] PixelData, int Width, int Height, int SamplesPerPixel,
            string PhotometricInterpretation, int BitsAllocated) GetPixelData(string filePath)
    {
        var file = DicomFile.Open(filePath);
        var dataset = file.Dataset;

        // Use fo-dicom's rendering to get normalized pixel data
        var dicomImage = new DicomImage(dataset);
        var renderedImage = dicomImage.RenderImage();

        // Use actual rendered dimensions (may differ from DICOM tags for some files)
        int width = renderedImage.Width;
        int height = renderedImage.Height;

        // Convert rendered image to byte array
        // fo-dicom renders to BGRA format, we need to convert appropriately
        byte[] pixels = ExtractPixelsFromRenderedImage(renderedImage, width, height, out int actualSamplesPerPixel);

        return (pixels, width, height, actualSamplesPerPixel, "RGB", 8);
    }

    private BitmapSource? CreateThumbnail(DicomFile file, int thumbnailHeight)
    {
        try
        {
            var bitmap = RenderDicomToBitmap(file);
            if (bitmap == null) return null;

            // Scale to thumbnail size
            double scale = thumbnailHeight / (double)bitmap.PixelHeight;
            var scaledBitmap = new TransformedBitmap(bitmap,
                new ScaleTransform(scale, scale));

            // Convert to frozen BitmapImage for thread safety
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaledBitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            var thumbnail = new BitmapImage();
            thumbnail.BeginInit();
            thumbnail.CacheOption = BitmapCacheOption.OnLoad;
            thumbnail.StreamSource = stream;
            thumbnail.EndInit();
            thumbnail.Freeze();

            return thumbnail;
        }
        catch
        {
            return null;
        }
    }

    private BitmapSource RenderDicomToBitmap(DicomFile file)
    {
        var dicomImage = new DicomImage(file.Dataset);
        var renderedImage = dicomImage.RenderImage();

        // fo-dicom's IImage to WPF BitmapSource
        int width = renderedImage.Width;
        int height = renderedImage.Height;

        // Get pixels as BGRA - Bgra32 format expects B, G, R, A byte order
        var pixels = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = renderedImage.GetPixel(x, y);
                // Pack as BGRA (little-endian: B is lowest byte)
                pixels[y * width + x] = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
            }
        }

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        bitmap.Freeze();

        return bitmap;
    }

    private byte[] ExtractPixelsFromRenderedImage(FellowOakDicom.Imaging.IImage renderedImage,
        int width, int height, out int samplesPerPixel)
    {
        // fo-dicom renders to Color32, we convert to RGB for burning
        samplesPerPixel = 3;
        var rgbPixels = new byte[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = renderedImage.GetPixel(x, y);
                int idx = (y * width + x) * 3;

                // Color32 to RGB
                rgbPixels[idx] = color.R;
                rgbPixels[idx + 1] = color.G;
                rgbPixels[idx + 2] = color.B;
            }
        }

        return rgbPixels;
    }
}
