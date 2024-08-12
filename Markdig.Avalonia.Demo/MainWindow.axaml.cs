using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Markdig.Avalonia.Helper;
using TextMateSharp.Grammars;

namespace Markdig.Avalonia.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

  
}