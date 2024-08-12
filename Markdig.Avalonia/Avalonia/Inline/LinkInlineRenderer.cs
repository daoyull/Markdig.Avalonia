using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Markdig.Avalonia.Controls;
using Markdig.Syntax.Inlines;

namespace Markdig.Avalonia.Avalonia.Inline;

public class LinkInlineRenderer : AvaloniaObjectRenderer<LinkInline>
{
    protected override void Write(AvaloniaRenderer renderer, LinkInline mark)
    {
        if (string.IsNullOrEmpty(mark.Url))
        {
            return;
        }

        if (mark.IsImage)
        {
            var border = new Border();
            border.Classes.Add(nameof(LinkInline) + "Image");
            var image = new ImageAsync();
            image.Url = mark.Url;
            image.Stretch = Stretch.UniformToFill;
            border.Child = image;
            renderer.WriteControl(border);
        }
        else
        {
            var hyperlinkButton = new HyperlinkButton();

            hyperlinkButton.Classes.Add("MarkHyperlinkButton");
            if (mark.Url?.StartsWith("http") == true)
            {
                hyperlinkButton.NavigateUri = new Uri(mark.Url);
            }


            var panel = new StackPanel();
            panel.Classes.Add(nameof(LinkInline) + "Text");
            renderer.Push(panel);


            var wrapPanel = new WrapPanel() { Orientation = Orientation.Horizontal };
            renderer.Push(wrapPanel);
            renderer.WriteChildren(mark);
            renderer.Pop(false);
            renderer.Pop(false);
            panel.Children.Add(wrapPanel);
            // 下划线
            var border = new Border();
            border.Classes.Add(nameof(LinkInline) + "TextUnderLine");
            panel.Children.Add(border);

            hyperlinkButton.Theme = new ControlTheme()
            {
                TargetType = typeof(HyperlinkButton)
            };
            hyperlinkButton.Content = panel;
            renderer.WriteControl(hyperlinkButton);
        }
    }
}