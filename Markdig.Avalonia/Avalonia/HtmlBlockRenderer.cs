using Markdig.Syntax;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace Markdig.Avalonia.Avalonia;

public class HtmlBlockRenderer :  AvaloniaObjectRenderer<HtmlBlock>
{
    protected override void Write(AvaloniaRenderer renderer, HtmlBlock obj)
    {
        var htmlPanel = new HtmlPanel();
        htmlPanel.Text = obj.Lines.ToString();
        renderer.WriteControl(htmlPanel);
    }
}