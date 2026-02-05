using System.Windows;
using System.Windows.Controls;
using PixBurn.ViewModels;

namespace PixBurn.Views;

public partial class AnnotationEditorView : UserControl
{
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
        }
    }
}
