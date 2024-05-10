namespace Avalonia_Text_Diff_Tool.ViewModels;

public class DiffViewModel : ViewModelBase
{
    private bool _realTimeDiffering = true;
    private bool _synchronousScrolling = true;
    private bool _lineAlignment = true;

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

    public bool LineAlignment
    {
        get => _lineAlignment;
        set
        {
            if (value == _lineAlignment) return;
            _lineAlignment = value;
            OnPropertyChanged();
        }
    }
}