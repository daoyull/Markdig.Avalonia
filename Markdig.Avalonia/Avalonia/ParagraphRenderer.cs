using Avalonia.Controls;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class ParagraphRenderer : AvaloniaObjectRenderer<ParagraphBlock>
{
    protected override void Write(AvaloniaRenderer renderer, ParagraphBlock obj)
    {
        var wrapPanel = new WrapPanel();
        wrapPanel.Classes.Add("Paragraph");
        renderer.Push(wrapPanel);
        renderer.WriteLeafInline(obj);
        renderer.Pop();
    }
}