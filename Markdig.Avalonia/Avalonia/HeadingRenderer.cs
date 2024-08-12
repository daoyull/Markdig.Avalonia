using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.LogicalTree;
using Markdig.Avalonia.Entity;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class HeadingRenderer : AvaloniaObjectRenderer<HeadingBlock>
{
    protected override void Write(AvaloniaRenderer renderer, HeadingBlock obj)
    {
        var stackPanel = new StackPanel();
        stackPanel.Name = "Heading" + obj.Level;
        stackPanel.Classes.Add("Heading" + obj.Level);
        var wrapPanel = new WrapPanel();
        renderer.Push(stackPanel);
        renderer.Push(wrapPanel);
        renderer.WriteLeafInline(obj);
        renderer.Pop();
        renderer.Pop();
        stackPanel.Children.Add(new Border(){Classes = { "HeaderUnderBorder" }});

        var logical = stackPanel.GetLogicalDescendants().FirstOrDefault(it => it.GetType() == typeof(TextBlock));
        if (logical != null && logical is TextBlock textBlock)
        {
            var stringBuilder = new StringBuilder();
            var router = new HeadingRouter();
            foreach (var inline in textBlock.Inlines!)
            {
                if (inline is Run run)
                {
                    stringBuilder.Append(run.Text);
                }
            }

            router.Text = stringBuilder.ToString();
            router.Level = obj.Level;
            router.Control = stackPanel;
            renderer.WriteTitleRouter(router);
        }
    }
}