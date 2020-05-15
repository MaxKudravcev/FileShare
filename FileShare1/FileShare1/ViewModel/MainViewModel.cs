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
        private FTPServer server;
        private bool isServerActive;
        private string ipCaption;

        public string IPCaption
        {
            get
            {
                return ipCaption;
            }
            set
            {
                ipCaption = value;
                OnPropertyChanged(nameof(IPCaption));
            }
        }

        public ICommand SwitchPageCommand { protected set; get; }

        public bool IsServerActive
        {
            get
            {
                return isServerActive;
            }
            set
            {
                if (value == true)
                {
                    server.Start();
                    string host = System.Net.Dns.GetHostName();
                    System.Net.IPAddress ip = System.Net.Dns.GetHostEntry(host).AddressList[0];
                    IPCaption = "Copy the following URL into your Windows file explorer/FTP-Client/Browser\n\nftp://" + ip.ToString() + ":2121";
                }
                else
                {
                    server.Stop();
                    IPCaption = "To start FTP-server turn on the switch above";
                }

                isServerActive = value;
                OnPropertyChanged(nameof(IsServerActive));
            }
        }

        public MainViewModel()
        {
            SwitchPageCommand = new RelayCommand<string>(SwitchPage);
            server = new FTPServer();
            isServerActive = false;
            ipCaption = "To start FTP-server turn on the switch above";
        }

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
