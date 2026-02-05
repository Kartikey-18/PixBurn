using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixBurn.Models;
using PixBurn.Services.Interfaces;

namespace PixBurn.ViewModels;

public partial class FileListViewModel : ObservableObject
{
    private readonly IDicomReader _reader;
    private readonly IFileDialogService _fileDialog;

    public ObservableCollection<DicomFileItem> Files { get; } = new();

    [ObservableProperty]
    private DicomFileItem? selectedFile;

    public bool HasFiles => Files.Count > 0;

    // Allow MainWindowViewModel to notify when save state changes
    public IRelayCommand? SaveCommand { get; set; }
    public IRelayCommand? SaveAsNewCommand { get; set; }

    public FileListViewModel(IDicomReader reader, IFileDialogService fileDialog)
    {
        _reader = reader;
        _fileDialog = fileDialog;

        Files.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasFiles));
            SaveCommand?.NotifyCanExecuteChanged();
            SaveAsNewCommand?.NotifyCanExecuteChanged();
        };
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var paths = _fileDialog.BrowseDicomFiles();
        if (paths is not null)
            AddFiles(paths);
    }

    [RelayCommand]
    private void ClearFiles()
    {
        Files.Clear();
        SelectedFile = null;
    }

    [RelayCommand]
    private void RemoveFile(DicomFileItem? file)
    {
        if (file is null) return;

        Files.Remove(file);
        if (SelectedFile == file)
            SelectedFile = Files.FirstOrDefault();
    }

    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var path in filePaths)
        {
            // Skip if already in list
            if (Files.Any(f => f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
                continue;

            // Validate file extension
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is not ".dcm" and not ".dicom")
                continue;

            try
            {
                var item = _reader.ReadFileInfo(path);
                Files.Add(item);

                // Auto-select first file
                SelectedFile ??= item;
            }
            catch
            {
                // Skip unreadable files
            }
        }
    }

    partial void OnSelectedFileChanged(DicomFileItem? value)
    {
        SaveCommand?.NotifyCanExecuteChanged();
        SaveAsNewCommand?.NotifyCanExecuteChanged();
    }
}
