using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixBurn.Models;
using PixBurn.Models.Annotations;
using PixBurn.Services.Interfaces;

namespace PixBurn.ViewModels;

public partial class AnnotationEditorViewModel : ObservableObject
{
    private readonly IDicomReader _reader;

    [ObservableProperty] private DicomFileItem? currentFile;
    [ObservableProperty] private BitmapSource? displayImage;
    [ObservableProperty] private AnnotationToolType selectedTool = AnnotationToolType.Select;
    [ObservableProperty] private AnnotationBase? selectedAnnotation;

    // Drawing state
    [ObservableProperty] private bool isDrawing;
    [ObservableProperty] private Point drawStartPoint;

    // Tool settings
    [ObservableProperty] private Color currentStrokeColor = Colors.Red;
    [ObservableProperty] private double currentStrokeWidth = 2.0;
    [ObservableProperty] private double currentFontSize = 14.0;

    // Color options for UI
    public static Color[] ColorOptions => new[]
    {
        Colors.Red,
        Colors.Yellow,
        Colors.Lime,
        Colors.Cyan,
        Colors.White,
        Colors.Black
    };

    public ObservableCollection<AnnotationBase> Annotations =>
        CurrentFile?.Annotations ?? _emptyAnnotations;

    private static readonly ObservableCollection<AnnotationBase> _emptyAnnotations = new();

    public AnnotationEditorViewModel(IDicomReader reader)
    {
        _reader = reader;
    }

    public void LoadFile(DicomFileItem? file)
    {
        CurrentFile = file;
        if (file is not null)
        {
            file.FullImage ??= _reader.LoadFullImage(file.FilePath);
            DisplayImage = file.FullImage;
        }
        else
        {
            DisplayImage = null;
        }
        OnPropertyChanged(nameof(Annotations));
        SelectedAnnotation = null;
    }

    public void RefreshImage()
    {
        if (CurrentFile is not null)
        {
            CurrentFile.FullImage = _reader.LoadFullImage(CurrentFile.FilePath);
            DisplayImage = CurrentFile.FullImage;
        }
    }

    // Called from canvas on mouse down
    public void StartDrawing(Point normalizedPoint)
    {
        if (CurrentFile is null) return;

        if (SelectedTool == AnnotationToolType.Select)
        {
            // Hit test for selection handled in canvas
            return;
        }

        IsDrawing = true;
        DrawStartPoint = normalizedPoint;

        if (SelectedTool == AnnotationToolType.Text)
        {
            // Text is placed immediately
            var textAnnotation = new TextAnnotation
            {
                Position = normalizedPoint,
                Text = "Text",
                FontSize = CurrentFontSize,
                StrokeColor = CurrentStrokeColor
            };
            AddAnnotation(textAnnotation);
            SelectedAnnotation = textAnnotation;
            IsDrawing = false;
        }
    }

    // Called from canvas on mouse up
    public void FinishDrawing(Point normalizedEndPoint)
    {
        if (!IsDrawing || CurrentFile is null) return;
        IsDrawing = false;

        // Ensure minimum size
        var dx = Math.Abs(normalizedEndPoint.X - DrawStartPoint.X);
        var dy = Math.Abs(normalizedEndPoint.Y - DrawStartPoint.Y);
        if (dx < 0.01 && dy < 0.01) return;

        AnnotationBase? newAnnotation = SelectedTool switch
        {
            AnnotationToolType.Arrow => new ArrowAnnotation
            {
                StartPoint = DrawStartPoint,
                EndPoint = normalizedEndPoint,
                StrokeColor = CurrentStrokeColor,
                StrokeWidth = CurrentStrokeWidth
            },
            AnnotationToolType.Rectangle => new RectangleAnnotation
            {
                Bounds = CreateRect(DrawStartPoint, normalizedEndPoint),
                StrokeColor = CurrentStrokeColor,
                StrokeWidth = CurrentStrokeWidth
            },
            _ => null
        };

        if (newAnnotation is not null)
        {
            AddAnnotation(newAnnotation);
            SelectedAnnotation = newAnnotation;
        }
    }

    private void AddAnnotation(AnnotationBase annotation)
    {
        Annotations.Add(annotation);
        if (CurrentFile is not null)
            CurrentFile.HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void DeleteAnnotation(AnnotationBase? annotation)
    {
        if (annotation is null || CurrentFile is null) return;
        Annotations.Remove(annotation);
        CurrentFile.HasUnsavedChanges = Annotations.Count > 0;
        if (SelectedAnnotation == annotation)
            SelectedAnnotation = null;
    }

    [RelayCommand]
    private void ClearAnnotations()
    {
        if (CurrentFile is null) return;
        Annotations.Clear();
        CurrentFile.HasUnsavedChanges = false;
        SelectedAnnotation = null;
    }

    public void SelectAnnotation(AnnotationBase? annotation)
    {
        // Deselect previous
        if (SelectedAnnotation is not null)
            SelectedAnnotation.IsSelected = false;

        SelectedAnnotation = annotation;

        // Select new
        if (annotation is not null)
            annotation.IsSelected = true;
    }

    private static Rect CreateRect(Point p1, Point p2)
    {
        var x = Math.Min(p1.X, p2.X);
        var y = Math.Min(p1.Y, p2.Y);
        var w = Math.Abs(p2.X - p1.X);
        var h = Math.Abs(p2.Y - p1.Y);
        return new Rect(x, y, w, h);
    }
}
