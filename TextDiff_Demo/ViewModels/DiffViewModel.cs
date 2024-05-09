using System.Windows.Input;
using Archon.Tool.Common.Mvvm;
using ReactiveUI;

namespace TextDiff_Demo.ViewModels;

public class DiffViewModel : ViewModelBase
{
    private bool _realTimeDiffering = true;
    
    public bool RealTimeDiffering
    {
        get => _realTimeDiffering;
        set
        {
            if (value == _realTimeDiffering) return;
            _realTimeDiffering = value;
            OnPropertyChanged();
        }
    }

    private bool _synchronousScrolling = true;

    public bool SynchronousScrolling
    {
        get => _synchronousScrolling;
        set
        {
            if (value == _synchronousScrolling) return;
            _synchronousScrolling = value;
            OnPropertyChanged();
        }
    }
}