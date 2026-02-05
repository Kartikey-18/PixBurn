using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixBurn.Models.Annotations;

public partial class TextAnnotation : AnnotationBase
{
    [ObservableProperty] private Point position;     // Top-left, normalized 0-1
    [ObservableProperty] private string text = "Text";
    [ObservableProperty] private double fontSize = 14.0;
    [ObservableProperty] private string fontFamily = "Arial";
    [ObservableProperty] private Color backgroundColor = Colors.Transparent;
    [ObservableProperty] private bool hasBackground = false;

    public override Rect GetBoundingBox()
    {
        // Approximate - actual size computed at render time
        return new Rect(Position.X, Position.Y, 0.2, 0.05);
    }

    public override AnnotationBase Clone() => new TextAnnotation
    {
        Position = Position,
        Text = Text,
        FontSize = FontSize,
        FontFamily = FontFamily,
        StrokeColor = StrokeColor,  // Text color
        BackgroundColor = BackgroundColor,
        HasBackground = HasBackground
    };
}
