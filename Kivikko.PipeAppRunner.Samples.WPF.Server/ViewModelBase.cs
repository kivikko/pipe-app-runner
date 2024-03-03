using System.ComponentModel;

namespace Kivikko.PipeAppRunner.Samples.WPF.Server;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}