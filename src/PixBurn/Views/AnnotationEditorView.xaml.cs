using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
}
