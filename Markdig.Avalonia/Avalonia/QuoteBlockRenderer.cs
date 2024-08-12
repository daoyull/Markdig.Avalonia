using Avalonia.Controls;
using Avalonia.Layout;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class QuoteBlockRenderer : AvaloniaObjectRenderer<QuoteBlock>
{
    protected override void Write(AvaloniaRenderer renderer, QuoteBlock quote)
    {
        var wrapPanel = new WrapPanel();
        wrapPanel.Classes.Add("QuotePanel");
        wrapPanel.Orientation = Orientation.Horizontal;

        var border = new Border();
        border.Classes.Add("QuoteBorder");
        wrapPanel.Children.Add(border);
        renderer.Push(wrapPanel);
        renderer.WriteChildren(quote);
        renderer.Pop();
    }
}