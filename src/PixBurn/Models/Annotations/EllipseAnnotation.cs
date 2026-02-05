using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixBurn.Models.Annotations;

public partial class EllipseAnnotation : AnnotationBase
{
    [ObservableProperty] private Rect bounds;  // Normalized 0-1 bounding box
    [ObservableProperty] private Color? fillColor;  // Optional fill

    public override Rect GetBoundingBox() => Bounds;

    public override AnnotationBase Clone() => new EllipseAnnotation
    {
        Bounds = Bounds,
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        FillColor = FillColor
    };
}
