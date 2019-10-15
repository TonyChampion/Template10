using Prism.Ioc;
using System;
using System.Threading.Tasks;
using Template10.Ioc;
using Template10.Navigation;
using Template10.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Template10.JumpList.Sample.Views;
using Template10.JumpList.Sample.ViewModels;
using Windows.UI.StartScreen;
using Windows.ApplicationModel.Activation;

namespace Template10.JumpList.Sample
{
    sealed partial class App : Template10.ApplicationBase
    {
        private INavigationService _nav;

        public App() => InitializeComponent();

        public override void RegisterTypes(IContainerRegistry container)
        {
            container.RegisterView<MainPage, MainPageViewModel>();
        }

        public override void OnInitialized()
        {
            var frame = new Frame();
            Window.Current.Content = new ShellPage { Frame = frame };
            Window.Current.Activate();
            _nav = NavigationFactory
                .Create(frame, Guid.Empty.ToString())
                .AttachGestures(Window.Current, Gesture.Back, Gesture.Forward, Gesture.Refresh);
        }

        public override async Task OnStartAsync(IStartArgs args)
        {
            await CreateJumpList();

            if (args.StartKind == StartKinds.Activate 
                && args.StartCause == StartCauses.JumpListItem
                && args.Arguments is LaunchActivatedEventArgs)
            {
                var jumpListArgs = (args.Arguments as LaunchActivatedEventArgs).Arguments;
                var navParams = new NavigationParameters(("key", jumpListArgs));

                await _nav.NavigateAsync(nameof(MainPage), navParams);
            } else
            {
                await _nav.NavigateAsync(nameof(MainPage));
            }
        }

        private async Task CreateJumpList()
        {
            var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();

            if(jumpList.Items.Count == 0)
            {
                jumpList.Items.Add(JumpListItem.CreateWithArguments("RecentFile1", "Recent File 1"));
                jumpList.Items.Add(JumpListItem.CreateWithArguments("RecentFile2", "Recent File 2"));
                jumpList.Items.Add(JumpListItem.CreateWithArguments("RecentFile3", "Recent File 3"));

                await jumpList.SaveAsync();

            }

        }
    }
}
