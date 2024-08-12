using Avalonia;
using Avalonia.Controls;
using Markdig.Avalonia.Entity;
using Markdig.Syntax;

namespace Markdig.Avalonia.Avalonia;

public class ListRenderer : AvaloniaObjectRenderer<ListBlock>
{
    protected override void Write(AvaloniaRenderer renderer, ListBlock mark)
    {
        var panel = new StackPanel();
        panel.Classes.Add(nameof(ListBlock));
        renderer.Push(panel);
        foreach (var item in mark)
        {
            var listItemBlock = (ListItemBlock)item;
            var grid = new Grid();
            grid.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            panel.Children.Add(grid);
            var listItem = new StackPanel();
            listItem.Classes.Add(nameof(ListItemBlock));
            var order = new TextBlock();
            if (mark.IsOrdered)
            {
                order.Text = listItemBlock.Order + ". ";
            }
            else
            {
                order.Text = "• ";
            }

            grid.Children.Add(order);
            grid.Children.Add(listItem);
            grid.Margin = new Thickness(5, 0, 0, 0);
            Grid.SetColumn(order, 0);
            Grid.SetColumn(listItem, 1);
            renderer.Push(listItem);
            renderer.WriteChildren(listItemBlock);
            renderer.Pop(false);
        }

        renderer.Pop();
    }
}