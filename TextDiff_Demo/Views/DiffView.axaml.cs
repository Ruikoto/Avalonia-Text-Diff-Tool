using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;
using TextDiff_Demo.Utils;
using TextDiff_Demo.ViewModels;

namespace TextDiff_Demo.Views
{
    public partial class DiffView : UserControl
    {
        public DiffView()
        {
            InitializeComponent();

            DataContext = new DiffViewModel();

            OlderEditor = OlderTextEditor;
            NewerEditor = NewerTextEditor;
            AddHighlightRenderer();
        }

        public TextEditor OlderEditor { get; }
        public TextEditor NewerEditor { get; }

        private void AddHighlightRenderer()
        {
            // 设置需要高亮的行号和行内的范围
            var linesToHighlight = new[] { 2, 3, 5 };
            var lineRangesToHighlight = new Dictionary<int, List<(int Start, int Length)>>
            {
                { 2, [(3, 5), (15, 4)] }, // 第2行，从索引3到5，以及索引15到4个字符
                { 3, [(10, 7)] },
                { 5, [(8, 10)] }
            };

            // 定义背景颜色刷
            IBrush lineBrush = new SolidColorBrush(Color.Parse("#FFDDDDDD"));
            IBrush rangeBrush = new SolidColorBrush(Color.Parse("#FFFFAACC"));

            // 创建并添加渲染器
            var highlightRenderer = new HighlightBackgroundRenderer(OlderEditor, linesToHighlight, lineRangesToHighlight, lineBrush, rangeBrush);
            NewerEditor.TextArea.TextView.BackgroundRenderers.Add(highlightRenderer);
        }
    }
}