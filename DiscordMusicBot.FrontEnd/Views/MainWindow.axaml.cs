using Avalonia.Controls;
using Avalonia.Interactivity;
using DiscordMusicBot.Client;
using DiscordMusicBot.FrontEnd.Controls;
using DiscordMusicBot.FrontEnd.ViewModels;
using System;

namespace DiscordMusicBot.FrontEnd.Views;

public partial class MainWindow : Window
{
    private readonly Runner.Program _botRunner;
    public MainWindow(MainViewModel viewModel, Runner.Program botRunner)
    {
        InitializeComponent();
        DataContext = viewModel;
        _botRunner = botRunner;
    }

    public override void Show()
    {
        TextBoxWriter writer = new TextBoxWriter(ConsoleOutput, ConsoleScroller);
        Console.SetOut(writer);
        base.Show();
    }

    public async void OnBotStartClick(object sender, RoutedEventArgs args)
    {
        await _botRunner.RunAsync();
    }

    public async void OnBotStopClick(object sender, RoutedEventArgs args)
    {
        await _botRunner.StopAsync();
    }
}
