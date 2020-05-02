using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FileShare1
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MenuItem : Grid
	{
		public MenuItem()
		{
			InitializeComponent();
		}


        private string commandParameter;
        public string CommandParameter
        {
            get { return commandParameter; }
            set
            {
                commandParameter = value;
                OnPropertyChanged();
            }
        }

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
                                                                           nameof(CommandParameter),
                                                                           typeof(string),
                                                                           typeof(MenuItem),
                                                                           string.Empty,
                                                                           propertyChanging: (bindable, oldValue, newValue) =>
                                                                           {
                                                                               var ctrl = (MenuItem)bindable;
                                                                               ctrl.CommandParameter = (string)newValue;
                                                                           },
                                                                           defaultBindingMode: BindingMode.OneWay);


        private string icon;
        public string Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                OnPropertyChanged();
            }
        }

        public static readonly BindableProperty IconProperty = BindableProperty.Create(
                                                                           nameof(Icon),
                                                                           typeof(string),
                                                                           typeof(MenuItem),
                                                                           string.Empty,
                                                                           propertyChanging: (bindable, oldValue, newValue) =>
                                                                           {
                                                                               var ctrl = (MenuItem)bindable;
                                                                               ctrl.Icon = (string)newValue;
                                                                           },
                                                                           defaultBindingMode: BindingMode.OneWay);



        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged();
            }
        }

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
                                                                           nameof(Text),
                                                                           typeof(string),
                                                                           typeof(MenuItem),
                                                                           string.Empty,
                                                                           propertyChanging: (bindable, oldValue, newValue) =>
                                                                           {
                                                                               var ctrl = (MenuItem)bindable;
                                                                               ctrl.Text = (string)newValue;
                                                                           },
                                                                           defaultBindingMode: BindingMode.OneWay);


        private ICommand command;
        public ICommand Command
        {
            get { return command; }
            set
            {
                command = value;
                OnPropertyChanged();
            }
        }

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
                                                                           nameof(Command),
                                                                           typeof(ICommand),
                                                                           typeof(MenuItem),
                                                                           null,
                                                                           propertyChanging: (bindable, oldValue, newValue) =>
                                                                           {
                                                                               var ctrl = (MenuItem)bindable;
                                                                               ctrl.Command = (ICommand)newValue;
                                                                           },
                                                                           defaultBindingMode: BindingMode.OneWay);
    }
}