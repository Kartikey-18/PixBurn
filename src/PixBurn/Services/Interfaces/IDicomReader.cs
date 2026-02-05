using System.Windows.Media.Imaging;
using PixBurn.Models;

namespace PixBurn.Services.Interfaces;

public interface IDicomReader
{
    /// <summary>
    /// Reads DICOM file and extracts metadata + thumbnail
    /// </summary>
    DicomFileItem ReadFileInfo(string filePath);

    /// <summary>
    /// Loads full-resolution image for editing
    /// </summary>
    BitmapSource LoadFullImage(string filePath);

    /// <summary>
    /// Gets raw pixel bytes and metadata for burning
    /// </summary>
    (byte[] PixelData, int Width, int Height, int SamplesPerPixel,
     string PhotometricInterpretation, int BitsAllocated) GetPixelData(string filePath);
}
