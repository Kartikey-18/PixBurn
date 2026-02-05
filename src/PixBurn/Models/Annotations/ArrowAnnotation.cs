using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixBurn.Models.Annotations;

public partial class ArrowAnnotation : AnnotationBase
{
    [ObservableProperty] private Point startPoint;  // Normalized 0-1
    [ObservableProperty] private Point endPoint;    // Normalized 0-1
    [ObservableProperty] private double arrowHeadSize = 12.0;

    public override Rect GetBoundingBox()
    {
        var minX = Math.Min(StartPoint.X, EndPoint.X);
        var minY = Math.Min(StartPoint.Y, EndPoint.Y);
        var maxX = Math.Max(StartPoint.X, EndPoint.X);
        var maxY = Math.Max(StartPoint.Y, EndPoint.Y);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public override AnnotationBase Clone() => new ArrowAnnotation
    {
        StartPoint = StartPoint,
        EndPoint = EndPoint,
        StrokeColor = StrokeColor,
        StrokeWidth = StrokeWidth,
        ArrowHeadSize = ArrowHeadSize
    };
}
