using System.ComponentModel;

namespace FileShare1.ViewModel
{
    /// <summary>
    /// A base view model, that implements INotifyPropertyChanged
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
