using System.Collections.Concurrent;
using Markdig.Avalonia.Entity;
using TextMateSharp.Grammars;

namespace Markdig.Avalonia.Helper;

public static class CodeThemeManager
{
    private static ConcurrentDictionary<ThemeName, GrammarCache> _dict = new();

    public static void AddTheme(ThemeName themeName)
    {
        if (_dict.ContainsKey(themeName))
        {
            return;
        }

        var grammarCache = new GrammarCache(themeName);
        _dict[themeName] = grammarCache;
    }

    public static GrammarCache GeTheme(ThemeName themeName)
    {
        if (_dict.TryGetValue(themeName, out var grammarCache))
        {
            return grammarCache;
        }

        AddTheme(themeName);
        return GeTheme(themeName);
    }
}