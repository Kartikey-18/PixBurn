using System.Windows;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Extensions.DependencyInjection;
using PixBurn.Services;
using PixBurn.Services.Interfaces;
using PixBurn.ViewModels;
using PixBurn.Views;

namespace PixBurn;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        // fo-dicom with ImageSharp
        services.AddFellowOakDicom()
                .AddImageManager<ImageSharpImageManager>();

        // Services
        services.AddSingleton<IDicomReader, DicomReaderService>();
        services.AddSingleton<IDicomWriter, DicomWriterService>();
        services.AddSingleton<IAnnotationBurner, AnnotationBurnerService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IUidGenerator, UidGenerator>();

        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<FileListViewModel>();
        services.AddTransient<AnnotationEditorViewModel>();
        services.AddTransient<SaveProgressViewModel>();

        Services = services.BuildServiceProvider();

        // Tell fo-dicom to use our service provider
        DicomSetupBuilder.UseServiceProvider(Services);

        // Create and show main window
        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainWindowViewModel>()
        };
        mainWindow.Show();
    }
}
