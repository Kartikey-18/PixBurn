using Microsoft.Win32;
using PixBurn.Services.Interfaces;

namespace PixBurn.Services;

public class FileDialogService : IFileDialogService
{
    public string[]? BrowseDicomFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select DICOM Files",
            Filter = "DICOM Files (*.dcm;*.dicom)|*.dcm;*.dicom|All Files (*.*)|*.*",
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }
}
