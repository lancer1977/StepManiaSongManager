using System.Windows;
using Prism.DryIoc;
using Prism.Ioc;
using Stepmania.Manager.Dialogs;
using Stepmania.Manager.Dialogs.Confirmation;
using Stepmania.Manager.Services;
using Stepmania.Manager.Views;

namespace Stepmania.Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            var w = Container.Resolve<Main>();
            return w;
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Main, MainViewModel>();
            containerRegistry.RegisterDialog<TextInputDialog, TextInputDialogViewModel>();
            containerRegistry.RegisterDialog<ConfirmDialog, ConfirmDialogViewModel>();
            //containerRegistry.RegisterSingleton<IMediaPlayer, NAudioPlayerHelper>();
            containerRegistry.RegisterSingleton<IMediaPlayer, MediaPlayerHelper>();

        }
        
    }
}
