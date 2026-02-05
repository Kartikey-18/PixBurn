using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;
using PixBurn.Models.Annotations;

namespace PixBurn.Models;

public class DicomFileItem
{
    public string FilePath { get; init; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSizeBytes { get; init; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int BitsAllocated { get; set; }
    public int SamplesPerPixel { get; set; }
    public string PhotometricInterpretation { get; set; } = string.Empty;
    public BitmapSource? Thumbnail { get; set; }

    // Full-resolution bitmap for editor (loaded on-demand)
    public BitmapSource? FullImage { get; set; }

    // Annotations for this file
    public ObservableCollection<AnnotationBase> Annotations { get; } = new();

    // Track if annotations have been modified
    public bool HasUnsavedChanges { get; set; }

    public string DimensionsText => $"{Width} x {Height}";

    public string FileSizeText => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:F1} KB"
        : $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
}
