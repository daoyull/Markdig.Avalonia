using System.Collections.Concurrent;
using Markdig.Avalonia.Helper;
using Markdig.Syntax;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;


namespace Markdig.Avalonia.Entity;

public class GrammarCache
{
    private readonly ConcurrentDictionary<string, IGrammar> _grammarDict = new();

    private readonly Theme _theme;

    public GrammarCache(ThemeName themeName)
    {
        var options = new RegistryOptions(themeName);
        var registry = new Registry(options);
        _theme = registry.GetTheme();

        var availableLanguages = options.GetAvailableLanguages();
        foreach (var language in availableLanguages)
        {
            var aliases = language.Aliases.ToHashSet();
            aliases.Add(language.Id);
            string scopeName = options.GetScopeByLanguageId(language.Id);
            var loadGrammar = registry.LoadGrammar(scopeName);
            foreach (var alias in aliases)
            {
                _grammarDict[alias] = loadGrammar;
            }
        }

        RegisterGrammar("shell", _grammarDict["sh"]);
    }

    public void RegisterGrammar(string alias, IGrammar grammar)
    {
        _grammarDict[alias] = grammar;
    }

    public IGrammar? GetGrammarByType(string? type)
    {
        if (string.IsNullOrEmpty(type) || !_grammarDict.TryGetValue(type, out var grammar))
        {
            return null;
        }

        return grammar;
    }


    public List<LineCode> RendererCode(FencedCodeBlock mark, string? type)
    {
        var code = mark.Lines.ToString();
        var lineCodes = new List<LineCode>();
        code = code.Replace("\r\n", "\n");

        var grammar = GetGrammarByType(type);

        if (grammar == null)
        {
            lineCodes.Add(new LineCode(code));
            return lineCodes;
        }

        ITokenizeLineResult result;
        lock (grammar)
        {
            result = grammar.TokenizeLine(code);
        }

        foreach (IToken token in result.Tokens)
        {
            int startIndex = (token.StartIndex > code.Length) ? code.Length : token.StartIndex;
            int endIndex = (token.EndIndex > code.Length) ? code.Length : token.EndIndex;
            int foreground = -1;
            int background = -1;
            foreach (var themeRule in _theme.Match(token.Scopes))
            {
                if (foreground == -1 && themeRule.foreground > 0)
                    foreground = themeRule.foreground;

                if (background == -1 && themeRule.background > 0)
                    background = themeRule.background;
            }

            var text = code.SubstringAtIndexes(startIndex, endIndex);
            var lineCode = new LineCode(text);
            lineCode.Foreground = foreground != -1 ? _theme.GetColor(foreground) : "#ffffff";
            lineCode.Background = background != -1 ? _theme.GetColor(background) : string.Empty;
            lineCodes.Add(lineCode);
        }

        if (lineCodes.Count > 0 && lineCodes.Last().Text == Environment.NewLine)
        {
            lineCodes.Remove(lineCodes.Last());
        }

        return lineCodes;
    }
}