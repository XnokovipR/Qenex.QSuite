using System.Windows;
using Qenex.QInsight.ViewModels;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.Wpf;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Docking;


namespace Qenex.QInsight.Views;

public class TelerikDockingPanesFactory : DockingPanesFactory
{
    protected override RadPane CreatePaneForItem(object item)
    {
        if (item is IViewModelBase viewModel)
        {
            var radPane = new QRadPane();//viewModel.IsDocument ? new QRadDocumentPane() : new QRadPane();
            radPane.DataContext = item;
            radPane.Name = viewModel.Name;
            radPane.Header = viewModel.Header;
            radPane.Tag = viewModel.DockPosition.ToString();
            radPane.CanUserClose = viewModel.IsDocument;


            // if (viewModel is IWorkspaceViewModel ws)
            // {
            //     radPane.Header = ws.WinTitle;
            //     radPane.GotFocus += ws.GotFocus;
            // }

            if (radPane is QRadPane qRadPane)
            {
                qRadPane.CustomTags = viewModel.CustomTags;
            }
            // else if (radPane is QRadDocumentPane qRadDocumentPane)
            // {
            //     qRadDocumentPane.Tags = viewModel.Tags;
            // }

            RadDocking.SetSerializationTag(radPane, viewModel.Header);
            UIElement shellView = ViewLocator.Locate(viewModel)!;
            radPane.Content = shellView;

            return radPane;
        }
        return base.CreatePaneForItem(item);
    }

    protected override void AddPane(Telerik.Windows.Controls.RadDocking radDocking, Telerik.Windows.Controls.RadPane pane)
    {
        var tag = pane.Tag.ToString();
        if (tag != null && radDocking.SplitItems.FirstOrDefault(i => i.Control.Name.Contains(tag)) is RadPaneGroup paneGroup)
        {
            paneGroup.Items.Add(pane);
        }
        else
        {
            base.AddPane(radDocking, pane);
        }
    }
}