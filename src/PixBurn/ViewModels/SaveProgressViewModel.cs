using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PixBurn.ViewModels;

public partial class SaveProgressViewModel : ObservableObject
{
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string currentFileName = string.Empty;

    private CancellationTokenSource? _cts;

    public CancellationToken GetCancellationToken()
    {
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    public void Reset()
    {
        IsSaving = false;
        CurrentFileName = string.Empty;
        _cts?.Dispose();
        _cts = null;
    }
}
