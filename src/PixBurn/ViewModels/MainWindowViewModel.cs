using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixBurn.Services.Interfaces;

namespace PixBurn.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDicomReader _reader;
    private readonly IDicomWriter _writer;
    private readonly IAnnotationBurner _burner;

    public FileListViewModel FileList { get; }
    public AnnotationEditorViewModel Editor { get; }
    public SaveProgressViewModel Progress { get; }

    [ObservableProperty]
    private string statusBarText = "Ready. Import DICOM files to begin.";

    public MainWindowViewModel(
        FileListViewModel fileList,
        AnnotationEditorViewModel editor,
        SaveProgressViewModel progress,
        IDicomReader reader,
        IDicomWriter writer,
        IAnnotationBurner burner)
    {
        FileList = fileList;
        Editor = editor;
        Progress = progress;
        _reader = reader;
        _writer = writer;
        _burner = burner;

        // Wire up commands for CanExecute updates
        FileList.SaveCommand = SaveCommand;
        FileList.SaveAsNewCommand = SaveAsNewCommand;

        // Wire up selection changes
        FileList.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FileList.SelectedFile))
            {
                Editor.LoadFile(FileList.SelectedFile);
                SaveCommand.NotifyCanExecuteChanged();
                SaveAsNewCommand.NotifyCanExecuteChanged();
            }
        };
    }

    private bool CanSave() =>
        FileList.SelectedFile is not null &&
        FileList.SelectedFile.Annotations.Count > 0 &&
        !Progress.IsSaving;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (FileList.SelectedFile is null) return;

        var file = FileList.SelectedFile;

        Progress.IsSaving = true;
        Progress.CurrentFileName = file.FileName;
        StatusBarText = $"Saving {file.FileName}...";

        try
        {
            var (pixels, w, h, spp, pi, _) = _reader.GetPixelData(file.FilePath);
            var burnedPixels = _burner.BurnAnnotations(
                pixels, w, h, spp, pi, file.Annotations.ToList());

            var result = await _writer.SaveAsync(
                file.FilePath, burnedPixels, w, h, 3, file.FilePath);

            if (result.Success)
            {
                file.HasUnsavedChanges = false;
                file.Annotations.Clear();

                // Reload the image to show burned annotations
                file.FullImage = null;
                file.Thumbnail = null;
                var updatedInfo = _reader.ReadFileInfo(file.FilePath);
                file.Thumbnail = updatedInfo.Thumbnail;

                Editor.RefreshImage();
                StatusBarText = $"Saved: {file.FileName}";
            }
            else
            {
                StatusBarText = $"Error saving: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusBarText = $"Error: {ex.Message}";
        }
        finally
        {
            Progress.Reset();
            SaveCommand.NotifyCanExecuteChanged();
            SaveAsNewCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsNewAsync()
    {
        if (FileList.SelectedFile is null) return;

        var file = FileList.SelectedFile;
        var dir = Path.GetDirectoryName(file.FilePath)!;
        var outputDir = Path.Combine(dir, "PixBurned");
        var outputPath = Path.Combine(outputDir, Path.GetFileName(file.FilePath));

        Progress.IsSaving = true;
        Progress.CurrentFileName = file.FileName;
        StatusBarText = $"Saving {file.FileName} to PixBurned...";

        try
        {
            var (pixels, w, h, spp, pi, _) = _reader.GetPixelData(file.FilePath);
            var burnedPixels = _burner.BurnAnnotations(
                pixels, w, h, spp, pi, file.Annotations.ToList());

            var result = await _writer.SaveAsync(
                file.FilePath, burnedPixels, w, h, 3, outputPath);

            if (result.Success)
            {
                file.HasUnsavedChanges = false;
                StatusBarText = $"Saved to: {outputPath}";
            }
            else
            {
                StatusBarText = $"Error saving: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusBarText = $"Error: {ex.Message}";
        }
        finally
        {
            Progress.Reset();
            SaveCommand.NotifyCanExecuteChanged();
            SaveAsNewCommand.NotifyCanExecuteChanged();
        }
    }
}
