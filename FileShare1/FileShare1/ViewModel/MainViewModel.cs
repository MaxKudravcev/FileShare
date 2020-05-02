using FileShare1.Model.FTPServer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace FileShare1.ViewModel
{
    class MainViewModel : BaseViewModel
    {
        public MainViewModel()
        {
            SwitchPageCommand = new RelayCommand<string>(SwitchPage);

            FTPServer server = new FTPServer();
            server.Start();
            int i = 1 + 1;
        }

        public ICommand SwitchPageCommand { protected set; get; }        



        private void SwitchPage(string pageName)
        {
            switch (pageName)
            {
                case "Share":
                    {
                        var masterDetailpage = App.Current.MainPage as MasterDetailPage;
                        masterDetailpage.Detail = new NavigationPage(new FileShare1.View.SharePage());
                        masterDetailpage.IsPresented = false;
                        break;
                    }
                case "About":
                    {
                        var masterDetailpage = App.Current.MainPage as MasterDetailPage;
                        masterDetailpage.Detail = new NavigationPage(new FileShare1.View.AboutPage());
                        masterDetailpage.IsPresented = false;
                        break;
                    }
            }
        }
    }
}
