using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Markdig.Avalonia.Helper;
using TextMateSharp.Grammars;

namespace Markdig.Avalonia.Demo;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        CodeThemeManager.AddTheme(ThemeName.DarkPlus);
    }


    [ObservableProperty] private string? _text;

    [RelayCommand]
    private async Task LoadMarkdown()
    {
        Text = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Markdown", "Demo.md"));
    }

    [RelayCommand]
    private void Empty()
    {
        Text = "";
    }
}