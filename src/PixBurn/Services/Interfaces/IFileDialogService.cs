namespace PixBurn.Services.Interfaces;

public interface IFileDialogService
{
    /// <summary>
    /// Opens file dialog for selecting DICOM files
    /// </summary>
    string[]? BrowseDicomFiles();
}
