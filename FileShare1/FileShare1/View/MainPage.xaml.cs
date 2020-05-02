using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using Xamarin.Forms;

namespace FileShare1
{
    public partial class MainPage : MasterDetailPage
    {
        public MainPage()
        {
            InitializeComponent();
            Detail = new NavigationPage(new View.AboutPage());
            BindingContext = new FileShare1.ViewModel.MainViewModel();
        }
    }
}
