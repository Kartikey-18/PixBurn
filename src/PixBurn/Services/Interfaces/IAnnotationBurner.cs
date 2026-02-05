using PixBurn.Models.Annotations;

namespace PixBurn.Services.Interfaces;

public interface IAnnotationBurner
{
    /// <summary>
    /// Burns annotations onto pixel data, returning new pixel buffer
    /// </summary>
    byte[] BurnAnnotations(
        byte[] sourcePixels,
        int width,
        int height,
        int samplesPerPixel,
        string photometricInterpretation,
        IReadOnlyList<AnnotationBase> annotations);
}
