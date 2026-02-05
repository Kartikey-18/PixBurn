using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixBurn.Models.Annotations;

public partial class RectangleAnnotation : AnnotationBase
{
    [ObservableProperty] private Rect bounds;  // Normalized 0-1
    [ObservableProperty] private Color? fillColor;  // Optional fill

    public override Rect GetBoundingBox() => Bounds;

    public override AnnotationBase Clone() => new RectangleAnnotation
    {
        Bounds = Bounds,
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        FillColor = FillColor
    };
}
