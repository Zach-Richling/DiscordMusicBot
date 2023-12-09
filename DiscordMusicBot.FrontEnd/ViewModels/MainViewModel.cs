using CommunityToolkit.Mvvm.ComponentModel;
using Discord;
using DiscordMusicBot.Client;
using DiscordMusicBot.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordMusicBot.FrontEnd.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly BotClient _botClient;
    public MainViewModel(BotClient botClient)
    {
        _botClient = botClient;
        _botClient.LoggedIn += LoginStatusChanged;
        _botClient.LoggedOut += LoginStatusChanged;
        _botClient.Connected += Connected;
        _botClient.Disconnected += Disconnected;
    }
    public string LoginStatus => _botClient.LoginStatus;
    public string ConnectionStatus => _botClient.ConnectionStatus;

    public IEnumerable<Tuple<IGuild, Song?, List<Song>>> AllQueues => _botClient.AllQueues.Select(x => Tuple.Create(x.Item1, x.Item2.FirstOrDefault(), x.Item2.Skip(1).ToList()));
    public string Greeting => "Welcome to the Discord Music Bot!";

    [ObservableProperty]
    private string connectionFailReason = string.Empty;

    private async Task LoginStatusChanged()
    {
        OnPropertyChanged(nameof(LoginStatus));
        await Task.CompletedTask;
    }

    private async Task Connected()
    {
        OnPropertyChanged(nameof(ConnectionStatus));
        ConnectionFailReason = string.Empty;
        await Task.CompletedTask;
    }

    private async Task Disconnected(Exception e)
    {
        OnPropertyChanged(nameof(ConnectionStatus));
        ConnectionFailReason = e.Message;
        await Task.CompletedTask;
    }

    public async Task QueueUpdated()
    {
        OnPropertyChanged(nameof(AllQueues));
        await Task.CompletedTask;
    }
}
