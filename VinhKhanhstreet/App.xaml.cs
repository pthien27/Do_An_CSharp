using Microsoft.Extensions.DependencyInjection;

namespace VinhKhanhstreet
{
    public partial class App : Application
    {
        public App()
        {
            try
            {
                InitializeComponent();
                MainPage = new NavigationPage(new VinhKhanhstreet.Pages.UserLoginPage());
            }
            catch (Exception ex)
            {
                // Hiện lỗi chi tiết hơn (lấy cả lỗi bên trong)
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                
                MainPage = new ContentPage { 
                    Content = new Label { 
                        Text = $"Startup Error: {errorMessage}", 
                        VerticalOptions = LayoutOptions.Center, 
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Padding = 20
                    } 
                };
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            // Đánh thức nhịp đập khi App quay trở lại
            try
            {
                if (MainPage is NavigationPage navPage)
                {
                    var currentPage = navPage.Navigation.NavigationStack.LastOrDefault();
                    if (currentPage is VinhKhanhstreet.Pages.MapPage mapPage)
                    {
                        mapPage.StartHeartbeat();
                    }
                }
            }
            catch { }
        }

        protected override void OnSleep()
        {
            base.OnSleep();

            // Gửi tín hiệu Offline ngay khi App đi vào chế độ ngủ (Chạy nền)
            try
            {
                if (MainPage is NavigationPage navPage)
                {
                    var currentPage = navPage.Navigation.NavigationStack.LastOrDefault();
                    if (currentPage is VinhKhanhstreet.Pages.MapPage mapPage)
                    {
                        mapPage.StopHeartbeat();
                    }
                }

                // KHÔNG DÙNG .Wait() ở đây để tránh treo App dẫn đến văng
            }
            catch { }
        }
    }
}