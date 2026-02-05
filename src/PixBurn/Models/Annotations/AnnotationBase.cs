using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixBurn.Models.Annotations;

public abstract partial class AnnotationBase : ObservableObject
{
    [ObservableProperty] private Guid id = Guid.NewGuid();
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private Color strokeColor = Colors.Red;
    [ObservableProperty] private double strokeWidth = 2.0;

    /// <summary>
    /// Gets the bounding box in normalized coordinates (0-1 range)
    /// </summary>
    public abstract Rect GetBoundingBox();

    /// <summary>
    /// Creates a deep copy of this annotation
    /// </summary>
    public abstract AnnotationBase Clone();
}
