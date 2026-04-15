using Microsoft.Extensions.DependencyInjection;

namespace VinhKhanhstreet
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new VinhKhanhstreet.Pages.UserLoginPage()));
        }
    }
}