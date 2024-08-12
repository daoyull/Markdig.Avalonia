using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Markdig.Avalonia.Entity;
using Markdig.Avalonia.Helper;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class FencedCodeRenderer : AvaloniaObjectRenderer<FencedCodeBlock>
{
    protected override void Write(AvaloniaRenderer renderer, FencedCodeBlock mark)
    {
        var panel = new Panel();

        async void CallBack(object? _)
        {
            await AddCodeBlock(panel, mark, renderer);
        }

        ThreadPool.QueueUserWorkItem(CallBack);
        var button = new Button();
        button.Click += async (sender, args) =>
        {
            if (sender is Button btn)
            {
                var clipboard = TopLevel.GetTopLevel(btn)?.Clipboard;
                if (clipboard != null)
                {
                    var dataObject = new DataObject();
                    dataObject.Set(DataFormats.Text, mark.Lines.ToString());
                    await clipboard.SetDataObjectAsync(dataObject);
                }
            }
        };
        button.Classes.Add("FencedCodeCopy");
        panel.Children.Add(button);

        var border = new Border()
        {
            Child = panel,
        };
        border.Classes.Add("FencedCode");
        renderer.WriteControl(border);
    }

    private async Task AddCodeBlock(Panel panel, FencedCodeBlock mark, AvaloniaRenderer markView)
    {
        int index = 0;
        while (index < 20)
        {
            var data = mark.GetData("LineCodes");
            if (data == null || data is not List<LineCode> codes)
            {
                await Task.Delay(50);
            }
            else
            {
                HandleLineCodes(panel, codes);
                break;
            }

            index++;
        }
    }

    private void HandleLineCodes(Panel panel, List<LineCode> lineCodes)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var textBlock = new TextBlock();
            textBlock.TextWrapping = TextWrapping.Wrap;
            foreach (var lineCode in lineCodes)
            {
                var run = new Run();
                run.Text = lineCode.Text;
                if (!string.IsNullOrEmpty(lineCode.Foreground))
                {
                    run.Foreground = SolidColorBrush.Parse(lineCode.Foreground);
                }

                if (!string.IsNullOrEmpty(lineCode.Background))
                {
                    run.Background = SolidColorBrush.Parse(lineCode.Background);
                }

                textBlock.Inlines?.Add(run);
            }

            panel.Children.Add(textBlock);
        });
    }
}