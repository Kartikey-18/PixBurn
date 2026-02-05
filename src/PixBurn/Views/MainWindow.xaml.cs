using System.IO;
using System.Windows;
using PixBurn.ViewModels;

namespace PixBurn.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            bool hasDicom = files.Any(f =>
            {
                var ext = Path.GetExtension(f).ToLowerInvariant();
                return ext is ".dcm" or ".dicom";
            });
            e.Effects = hasDicom ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        var dicomFiles = files.Where(f =>
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            return ext is ".dcm" or ".dicom";
        });

        if (DataContext is MainWindowViewModel vm)
        {
            vm.FileList.AddFiles(dicomFiles);
        }
    }
}
