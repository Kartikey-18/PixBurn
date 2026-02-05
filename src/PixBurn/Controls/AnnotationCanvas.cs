using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PixBurn.Models;
using PixBurn.Models.Annotations;

namespace PixBurn.Controls;

public class AnnotationCanvas : Canvas
{
    #region Dependency Properties

    public static readonly DependencyProperty AnnotationsProperty =
        DependencyProperty.Register(
            nameof(Annotations),
            typeof(ObservableCollection<AnnotationBase>),
            typeof(AnnotationCanvas),
            new PropertyMetadata(null, OnAnnotationsChanged));

    public static readonly DependencyProperty SelectedToolProperty =
        DependencyProperty.Register(
            nameof(SelectedTool),
            typeof(AnnotationToolType),
            typeof(AnnotationCanvas),
            new PropertyMetadata(AnnotationToolType.Select));

    public static readonly DependencyProperty SelectedAnnotationProperty =
        DependencyProperty.Register(
            nameof(SelectedAnnotation),
            typeof(AnnotationBase),
            typeof(AnnotationCanvas),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedAnnotationChanged));

    public static readonly DependencyProperty CurrentStrokeColorProperty =
        DependencyProperty.Register(
            nameof(CurrentStrokeColor),
            typeof(Color),
            typeof(AnnotationCanvas),
            new PropertyMetadata(Colors.Red));

    public static readonly DependencyProperty CurrentStrokeWidthProperty =
        DependencyProperty.Register(
            nameof(CurrentStrokeWidth),
            typeof(double),
            typeof(AnnotationCanvas),
            new PropertyMetadata(2.0));

    public ObservableCollection<AnnotationBase>? Annotations
    {
        get => (ObservableCollection<AnnotationBase>?)GetValue(AnnotationsProperty);
        set => SetValue(AnnotationsProperty, value);
    }

    public AnnotationToolType SelectedTool
    {
        get => (AnnotationToolType)GetValue(SelectedToolProperty);
        set => SetValue(SelectedToolProperty, value);
    }

    public AnnotationBase? SelectedAnnotation
    {
        get => (AnnotationBase?)GetValue(SelectedAnnotationProperty);
        set => SetValue(SelectedAnnotationProperty, value);
    }

    public Color CurrentStrokeColor
    {
        get => (Color)GetValue(CurrentStrokeColorProperty);
        set => SetValue(CurrentStrokeColorProperty, value);
    }

    public double CurrentStrokeWidth
    {
        get => (double)GetValue(CurrentStrokeWidthProperty);
        set => SetValue(CurrentStrokeWidthProperty, value);
    }

    #endregion

    #region Events

    public event Action<Point>? DrawingStarted;
    public event Action<Point>? DrawingFinished;
    public event Action<AnnotationBase?>? AnnotationSelected;
    public event Action? AnnotationMoved;
    public event Action<TextAnnotation>? TextAnnotationDoubleClicked;

    #endregion

    // Drawing state
    private bool _isDrawing;
    private Point _drawStart;
    private Point _drawCurrent;

    // Dragging state
    private bool _isDragging;
    private Point _dragStart;
    private AnnotationBase? _draggingAnnotation;

    public AnnotationCanvas()
    {
        ClipToBounds = true;
        Background = Brushes.Transparent; // Needed for hit testing
    }

    #region Property Changed Handlers

    private static void OnAnnotationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AnnotationCanvas canvas) return;

        if (e.OldValue is ObservableCollection<AnnotationBase> oldCollection)
            oldCollection.CollectionChanged -= canvas.OnAnnotationsCollectionChanged;

        if (e.NewValue is ObservableCollection<AnnotationBase> newCollection)
            newCollection.CollectionChanged += canvas.OnAnnotationsCollectionChanged;

        canvas.InvalidateVisual();
    }

    private void OnAnnotationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private static void OnSelectedAnnotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnnotationCanvas canvas)
            canvas.InvalidateVisual();
    }

    #endregion

    #region Mouse Handling

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        var pos = e.GetPosition(this);
        var normalized = NormalizePoint(pos);

        // Handle double-click for text editing
        if (e.ClickCount == 2 && SelectedTool == AnnotationToolType.Select)
        {
            var hit = HitTestAnnotation(normalized);
            if (hit is TextAnnotation textAnnotation)
            {
                TextAnnotationDoubleClicked?.Invoke(textAnnotation);
                e.Handled = true;
                return;
            }
        }

        if (SelectedTool == AnnotationToolType.Select)
        {
            var hit = HitTestAnnotation(normalized);
            SelectedAnnotation = hit;
            AnnotationSelected?.Invoke(hit);

            // Start dragging if we hit an annotation
            if (hit is not null)
            {
                _isDragging = true;
                _dragStart = normalized;
                _draggingAnnotation = hit;
                CaptureMouse();
            }
            return;
        }

        _isDrawing = true;
        _drawStart = normalized;
        _drawCurrent = normalized;

        DrawingStarted?.Invoke(normalized);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var pos = e.GetPosition(this);
        var normalized = NormalizePoint(pos);

        if (_isDragging && _draggingAnnotation is not null)
        {
            // Calculate delta
            var deltaX = normalized.X - _dragStart.X;
            var deltaY = normalized.Y - _dragStart.Y;

            // Move the annotation
            MoveAnnotation(_draggingAnnotation, deltaX, deltaY);
            _dragStart = normalized;

            InvalidateVisual();
        }
        else if (_isDrawing)
        {
            _drawCurrent = normalized;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_isDragging)
        {
            _isDragging = false;
            _draggingAnnotation = null;
            ReleaseMouseCapture();
            AnnotationMoved?.Invoke();
            return;
        }

        if (_isDrawing)
        {
            _isDrawing = false;
            ReleaseMouseCapture();

            var endPoint = NormalizePoint(e.GetPosition(this));
            DrawingFinished?.Invoke(endPoint);
            InvalidateVisual();
        }
    }

    private void MoveAnnotation(AnnotationBase annotation, double deltaX, double deltaY)
    {
        switch (annotation)
        {
            case TextAnnotation text:
                text.Position = new Point(
                    Math.Clamp(text.Position.X + deltaX, 0, 1),
                    Math.Clamp(text.Position.Y + deltaY, 0, 1));
                break;

            case ArrowAnnotation arrow:
                arrow.StartPoint = new Point(
                    Math.Clamp(arrow.StartPoint.X + deltaX, 0, 1),
                    Math.Clamp(arrow.StartPoint.Y + deltaY, 0, 1));
                arrow.EndPoint = new Point(
                    Math.Clamp(arrow.EndPoint.X + deltaX, 0, 1),
                    Math.Clamp(arrow.EndPoint.Y + deltaY, 0, 1));
                break;

            case RectangleAnnotation rect:
                var newX = Math.Clamp(rect.Bounds.X + deltaX, 0, 1 - rect.Bounds.Width);
                var newY = Math.Clamp(rect.Bounds.Y + deltaY, 0, 1 - rect.Bounds.Height);
                rect.Bounds = new Rect(newX, newY, rect.Bounds.Width, rect.Bounds.Height);
                break;
        }
    }

    private Point NormalizePoint(Point screenPoint)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
            return new Point(0, 0);

        return new Point(
            Math.Clamp(screenPoint.X / ActualWidth, 0, 1),
            Math.Clamp(screenPoint.Y / ActualHeight, 0, 1));
    }

    private Point DenormalizePoint(Point normalized)
    {
        return new Point(
            normalized.X * ActualWidth,
            normalized.Y * ActualHeight);
    }

    #endregion

    #region Rendering

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // Draw existing annotations
        if (Annotations is not null)
        {
            foreach (var annotation in Annotations)
            {
                RenderAnnotation(dc, annotation, annotation == SelectedAnnotation);
            }
        }

        // Draw preview while drawing
        if (_isDrawing && SelectedTool != AnnotationToolType.Text)
        {
            RenderPreview(dc);
        }
    }

    private void RenderAnnotation(DrawingContext dc, AnnotationBase annotation, bool isSelected)
    {
        var strokeBrush = new SolidColorBrush(annotation.StrokeColor);
        var strokeWidth = annotation.StrokeWidth;

        if (isSelected)
            strokeWidth += 1;

        var pen = new Pen(strokeBrush, strokeWidth);

        switch (annotation)
        {
            case ArrowAnnotation arrow:
                RenderArrow(dc, arrow, pen, strokeBrush);
                break;
            case RectangleAnnotation rect:
                RenderRectangle(dc, rect, pen);
                break;
            case TextAnnotation text:
                RenderText(dc, text, isSelected);
                break;
        }

        // Draw selection handles
        if (isSelected)
        {
            DrawSelectionHandles(dc, annotation);
        }
    }

    private void RenderArrow(DrawingContext dc, ArrowAnnotation arrow, Pen pen, Brush brush)
    {
        var start = DenormalizePoint(arrow.StartPoint);
        var end = DenormalizePoint(arrow.EndPoint);

        // Draw line
        dc.DrawLine(pen, start, end);

        // Draw arrowhead
        double headSize = arrow.ArrowHeadSize;
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);

        if (length > 1)
        {
            double ux = dx / length;
            double uy = dy / length;
            double px = -uy;
            double py = ux;

            double arrowBack = headSize * 0.8;
            double arrowWidth = headSize * 0.4;

            var p1 = new Point(
                end.X - ux * arrowBack + px * arrowWidth,
                end.Y - uy * arrowBack + py * arrowWidth);
            var p2 = new Point(
                end.X - ux * arrowBack - px * arrowWidth,
                end.Y - uy * arrowBack - py * arrowWidth);

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(end, true, true);
                ctx.LineTo(p1, true, false);
                ctx.LineTo(p2, true, false);
            }
            dc.DrawGeometry(brush, null, geometry);
        }
    }

    private void RenderRectangle(DrawingContext dc, RectangleAnnotation rect, Pen pen)
    {
        var topLeft = DenormalizePoint(new Point(rect.Bounds.X, rect.Bounds.Y));
        var bottomRight = DenormalizePoint(new Point(
            rect.Bounds.X + rect.Bounds.Width,
            rect.Bounds.Y + rect.Bounds.Height));

        var bounds = new Rect(topLeft, bottomRight);

        Brush? fill = rect.FillColor.HasValue
            ? new SolidColorBrush(rect.FillColor.Value)
            : null;

        dc.DrawRectangle(fill, pen, bounds);
    }

    private void RenderText(DrawingContext dc, TextAnnotation text, bool isSelected)
    {
        var position = DenormalizePoint(text.Position);

        var formattedText = new FormattedText(
            text.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(text.FontFamily),
            text.FontSize,
            new SolidColorBrush(text.StrokeColor),
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        if (text.HasBackground)
        {
            var bgRect = new Rect(
                position.X - 2,
                position.Y - 2,
                formattedText.Width + 4,
                formattedText.Height + 4);
            dc.DrawRectangle(new SolidColorBrush(text.BackgroundColor), null, bgRect);
        }

        dc.DrawText(formattedText, position);
    }

    private void RenderPreview(DrawingContext dc)
    {
        var pen = new Pen(new SolidColorBrush(CurrentStrokeColor), CurrentStrokeWidth)
        {
            DashStyle = DashStyles.Dash
        };

        var start = DenormalizePoint(_drawStart);
        var current = DenormalizePoint(_drawCurrent);

        switch (SelectedTool)
        {
            case AnnotationToolType.Arrow:
                dc.DrawLine(pen, start, current);
                break;

            case AnnotationToolType.Rectangle:
                var rect = new Rect(
                    Math.Min(start.X, current.X),
                    Math.Min(start.Y, current.Y),
                    Math.Abs(current.X - start.X),
                    Math.Abs(current.Y - start.Y));
                dc.DrawRectangle(null, pen, rect);
                break;
        }
    }

    private void DrawSelectionHandles(DrawingContext dc, AnnotationBase annotation)
    {
        var bounds = annotation.GetBoundingBox();
        var topLeft = DenormalizePoint(new Point(bounds.X, bounds.Y));
        var bottomRight = DenormalizePoint(new Point(bounds.Right, bounds.Bottom));

        var handleBrush = Brushes.White;
        var handlePen = new Pen(Brushes.Blue, 1);
        const double handleSize = 6;

        // Draw corner handles
        dc.DrawRectangle(handleBrush, handlePen,
            new Rect(topLeft.X - handleSize / 2, topLeft.Y - handleSize / 2, handleSize, handleSize));
        dc.DrawRectangle(handleBrush, handlePen,
            new Rect(bottomRight.X - handleSize / 2, topLeft.Y - handleSize / 2, handleSize, handleSize));
        dc.DrawRectangle(handleBrush, handlePen,
            new Rect(topLeft.X - handleSize / 2, bottomRight.Y - handleSize / 2, handleSize, handleSize));
        dc.DrawRectangle(handleBrush, handlePen,
            new Rect(bottomRight.X - handleSize / 2, bottomRight.Y - handleSize / 2, handleSize, handleSize));
    }

    #endregion

    #region Hit Testing

    private AnnotationBase? HitTestAnnotation(Point normalizedPoint)
    {
        if (Annotations is null) return null;

        // Check in reverse order (top to bottom in z-order)
        foreach (var annotation in Annotations.Reverse())
        {
            var bounds = annotation.GetBoundingBox();
            bounds.Inflate(0.02, 0.02); // Add tolerance

            if (bounds.Contains(normalizedPoint))
                return annotation;
        }
        return null;
    }

    #endregion
}
