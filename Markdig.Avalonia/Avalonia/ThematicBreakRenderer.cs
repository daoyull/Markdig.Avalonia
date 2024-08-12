using Avalonia.Controls;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class ThematicBreakRenderer : AvaloniaObjectRenderer<ThematicBreakBlock>
{
    protected override void Write(AvaloniaRenderer renderer, ThematicBreakBlock obj)
    {
        var border = new Border();
        border.Classes.Add("ThematicBreak");
        renderer.WriteControl(border);
    }
}