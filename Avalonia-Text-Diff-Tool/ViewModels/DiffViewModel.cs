using System;
using System.Windows.Input;
using Avalonia_Text_Diff_Tool.Utils;

namespace Avalonia_Text_Diff_Tool.ViewModels;

public class DiffViewModel : ViewModelBase
{
    private bool _realTimeDiffering = true;
    private bool _synchronousScrolling = true;
    private bool _lineAlignment = true;
    private bool _enableRealTimeDifferingButton = true;
    private bool _enableRenderButton;
    public event Action? DoClearDiff;

    public event Action? DoRender;

    public ICommand RenderCommand { get; set; }

    public DiffViewModel()
    {
        RenderCommand = new RelayCommand(p=> DoRender?.Invoke());
    }

    public bool RealTimeDiffering
    {
        get => _realTimeDiffering;
        set
        {
            if (value == _realTimeDiffering) return;
            _realTimeDiffering = value;
            EnableRenderButton = !value;
            OnPropertyChanged();
        }
    }

    public bool EnableRenderButton
    {
        get => _enableRenderButton;
        set
        {
            if (value == _enableRenderButton) return;
            _enableRenderButton = value;
            OnPropertyChanged();
        }
    }

    public bool EnableRealTimeDifferingButton
    {
        get => _enableRealTimeDifferingButton;
        set
        {
            if (value == _enableRealTimeDifferingButton) return;
            _enableRealTimeDifferingButton = value;
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
            if (value)
            {
                RealTimeDiffering = true;
                EnableRenderButton = false;
                EnableRealTimeDifferingButton = true;
                DoRender?.Invoke();
            }
            else
            {
                RealTimeDiffering = false;
                EnableRenderButton = false;
                EnableRealTimeDifferingButton = false;
                DoClearDiff?.Invoke();
            }

            OnPropertyChanged();
        }
    }
}