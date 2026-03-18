using VinhKhanhstreet.Models;
using VinhKhanhstreet.PageModels;

namespace VinhKhanhstreet.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}