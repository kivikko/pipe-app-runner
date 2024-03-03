using System.ComponentModel;

namespace Kivikko.PipeAppRunner.Samples.WPF.Client;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}