namespace Avalonia_Text_Diff_Tool.ViewModels;

public class DiffViewModel : ViewModelBase
{
    private bool _realTimeDiffering = true;

    private bool _synchronousScrolling = true;

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