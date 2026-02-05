using PixBurn.Models;

namespace PixBurn.Services.Interfaces;

public interface IDicomWriter
{
    /// <summary>
    /// Saves DICOM with new pixel data, preserving all original tags
    /// </summary>
    Task<SaveResult> SaveAsync(
        string sourcePath,
        byte[] newPixelData,
        int width,
        int height,
        int samplesPerPixel,
        string outputPath,
        CancellationToken ct = default);
}
