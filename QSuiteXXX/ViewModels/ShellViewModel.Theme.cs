using System.Windows;
using Qenex.QSuite.Models.AppSettings;

namespace Qenex.QSuite.ViewModels;

public partial class ShellViewModel
{
    #region Theme settings

    private void SetSyncfusionTheme(DesignManager designManager)
    {
        var win = Application.Current.MainWindow;

        var skinName = $"Fluent{designManager.Skin.ToString()}";

        int fontSize = designManager.FontSize < 6 || designManager.FontSize > 18 ? 12 : designManager.FontSize;
        if (skinName.Contains("Dark"))
        {
            SetFluentThemeDark(fontSize);
        }
        else
        {
            SetFluentThemeLight(fontSize);
        }

        //SfSkinManager.SetTheme(win, new Theme(skinName));
        //SfSkinManager.ApplyStylesOnApplication = true;
    }

    private void SetFluentThemeLight(int fontSize)
    {
        //FluentLightThemeSettings themeSettings = new FluentLightThemeSettings();

        ////themeSettings.PrimaryBackground = new SolidColorBrush(Colors.Red);
        ////themeSettings.PrimaryForeground = new SolidColorBrush(Colors.AntiqueWhite);
        //themeSettings.BodyFontSize = fontSize;
        //themeSettings.HeaderFontSize = fontSize;
        //themeSettings.SubHeaderFontSize = fontSize;
        //themeSettings.TitleFontSize = fontSize;
        //themeSettings.SubTitleFontSize = fontSize;
        //themeSettings.BodyAltFontSize = fontSize;
        ////themeSettings.FontFamily = new FontFamily("Callibri");
        //SfSkinManager.RegisterThemeSettings("FluentLight", themeSettings);
    }

    private void SetFluentThemeDark(int fontSize)
    {
        //FluentDarkThemeSettings themeSettings = new FluentDarkThemeSettings();
        ////themeSettings.PrimaryBackground = new SolidColorBrush(Colors.Red);
        ////themeSettings.PrimaryForeground = new SolidColorBrush(Colors.AntiqueWhite);
        //themeSettings.BodyFontSize = fontSize;
        //themeSettings.HeaderFontSize = fontSize;
        //themeSettings.SubHeaderFontSize = fontSize;
        //themeSettings.TitleFontSize = fontSize;
        //themeSettings.SubTitleFontSize = fontSize;
        //themeSettings.BodyAltFontSize = fontSize;
        ////themeSettings.FontFamily = new FontFamily("Callibri");
        //SfSkinManager.RegisterThemeSettings("FluentDark", themeSettings);
    }

    #endregion    
}