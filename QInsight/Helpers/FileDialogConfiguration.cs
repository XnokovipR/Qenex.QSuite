using System.IO;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Animation;
using Telerik.Windows.Controls.FileDialogs;

namespace Qenex.QInsight.Helpers;

public static class FileDialogConfiguration
{
    public static void PrepareFileDialog(DialogWindowBase dialog, string initialDirectory)
    {
        dialog.Owner = App.Current.MainWindow;
        dialog.InitialDirectory = initialDirectory;
        ConfigureFastFileDialog(dialog);
    }

    public static void ConfigureFastFileDialog(DialogWindowBase dialog)
    {
        dialog.LoadDrivesInBackground = true;
        // Faster dialog opening; trade-off: the breadcrumb shows "This PC" instead of the current path (Telerik limitation).
        dialog.ExpandToCurrentDirectory = false;
        // Details stays readable at large theme fonts; the Tiles layout overlaps its labels there.
        dialog.InitialSelectedLayout = LayoutType.Details;
        dialog.CanUserRename = false;
        AnimationManager.SetIsAnimationEnabled(dialog, false);
    }

    public static string GetInitialDialogDirectory(string? lastDirectory, string? currentFilePath)
    {
        if (Directory.Exists(lastDirectory))
        {
            return lastDirectory;
        }

        if (!string.IsNullOrWhiteSpace(currentFilePath))
        {
            var currentDirectory = Path.GetDirectoryName(currentFilePath);
            if (Directory.Exists(currentDirectory))
            {
                return currentDirectory;
            }
        }

        if (Directory.Exists(Environment.CurrentDirectory))
        {
            return Environment.CurrentDirectory;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
