using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PixBurn.ViewModels;

namespace PixBurn.Views;

public partial class AnnotationEditorView : UserControl
{
    private bool _isPanning;
    private Point _panStart;
    private double _scrollStartH;
    private double _scrollStartV;

    public AnnotationEditorView()
    {
        InitializeComponent();

        // Wire up canvas events to ViewModel
        AnnotationCanvas.DrawingStarted += OnDrawingStarted;
        AnnotationCanvas.DrawingFinished += OnDrawingFinished;
        AnnotationCanvas.AnnotationSelected += OnAnnotationSelected;
    }

    private void OnDrawingStarted(Point normalizedPoint)
    {
        if (DataContext is AnnotationEditorViewModel vm)
        {
            vm.StartDrawing(normalizedPoint);
        }
    }

    private void OnDrawingFinished(Point normalizedPoint)
    {
        if (DataContext is AnnotationEditorViewModel vm)
        {
            vm.FinishDrawing(normalizedPoint);
        }
    }

    private void OnAnnotationSelected(Models.Annotations.AnnotationBase? annotation)
    {
        if (DataContext is AnnotationEditorViewModel vm)
        {
            vm.SelectAnnotation(annotation);

            // If a text annotation is selected, start editing
            if (annotation is Models.Annotations.TextAnnotation textAnnotation)
            {
                vm.StartEditingText(textAnnotation);
                Dispatcher.BeginInvoke(() =>
                {
                    TextEditBox.Focus();
                    TextEditBox.SelectAll();
                });
            }
        }
    }

    private void TextEditBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (DataContext is AnnotationEditorViewModel vm)
            {
                vm.CancelEditingText();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            if (DataContext is AnnotationEditorViewModel vm)
            {
                vm.FinishEditingText();
            }
            e.Handled = true;
        }
    }

    private void TextEditBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is AnnotationEditorViewModel vm && vm.IsEditingText)
        {
            vm.FinishEditingText();
        }
    }

    #region Zoom and Pan

    private void ImageScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (DataContext is AnnotationEditorViewModel vm)
            {
                vm.HandleMouseWheel(e.Delta, true);
                e.Handled = true;
            }
        }
    }

    private void ImageScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Middle mouse button or Space+Left click for panning
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.Space)))
        {
            _isPanning = true;
            _panStart = e.GetPosition(ImageScrollViewer);
            _scrollStartH = ImageScrollViewer.HorizontalOffset;
            _scrollStartV = ImageScrollViewer.VerticalOffset;
            ImageScrollViewer.Cursor = Cursors.Hand;
            ImageScrollViewer.CaptureMouse();
            e.Handled = true;
        }
    }

    private void ImageScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ImageScrollViewer.Cursor = Cursors.Arrow;
            ImageScrollViewer.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void ImageScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var pos = e.GetPosition(ImageScrollViewer);
            var deltaX = pos.X - _panStart.X;
            var deltaY = pos.Y - _panStart.Y;

            ImageScrollViewer.ScrollToHorizontalOffset(_scrollStartH - deltaX);
            ImageScrollViewer.ScrollToVerticalOffset(_scrollStartV - deltaY);
            e.Handled = true;
        }
    }

    #endregion
}
