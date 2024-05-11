using System;
using System.Windows.Input;
using Avalonia_Text_Diff_Tool.Utils;

namespace Avalonia_Text_Diff_Tool.ViewModels;

public class DiffViewModel : ViewModelBase
{
    private bool _enableDiff = true;
    private bool _enableRealTimeDifferingButton = true;
    private bool _enableRenderButton;
    private bool _realTimeDiffering = true;
    private bool _synchronousScrolling = true;

    public DiffViewModel()
    {
        RenderCommand = new RelayCommand(p => DoRender?.Invoke());
    }

    public ICommand RenderCommand { get; set; }

    public bool RealTimeDiffering
    {
        get => _realTimeDiffering;
        set
        {
            if (value == _realTimeDiffering) return;
            _realTimeDiffering = value;
            EnableRenderButton = !value;
            if (value) DoRender?.Invoke();
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

    public bool EnableDiff
    {
        get => _enableDiff;
        set
        {
            if (value == _enableDiff) return;
            _enableDiff = value;
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

    public event Action? DoClearDiff;

    public event Action? DoRender;
}