using Avalonia.Controls;
using Avalonia.Media;
using TextDiff_Demo.Utils;
using TextDiff_Demo.ViewModels;

namespace TextDiff_Demo.Views
{
    public partial class DiffView : UserControl
    {
        public DiffView()
        {
            DiffViewModel vm = new();
            DataContext = vm;
            InitializeComponent();
            AddHighlightRenderer();
        }

        private void AddHighlightRenderer()
        {
            // 设定要高亮的行号
            int lineNumber = 3;

            // 创建背景颜色刷
            IBrush lineBrush = new SolidColorBrush(Color.Parse("#FFDDDDDD"));

            // 创建并添加渲染器
            var highlightRenderer = new HighlightBackgroundRenderer(textEditor, lineNumber, lineBrush);
            textEditor.TextArea.TextView.BackgroundRenderers.Add(highlightRenderer);
        }
    }
}