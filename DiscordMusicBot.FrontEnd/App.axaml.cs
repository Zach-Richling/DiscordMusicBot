using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using DiscordMusicBot.FrontEnd.ViewModels;
using DiscordMusicBot.FrontEnd.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;

namespace DiscordMusicBot.FrontEnd;

public partial class App : Application
{
    private static IServiceProvider _services = ConfigureServiceProvider();
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private static IServiceProvider ConfigureServiceProvider()
    {
        var services = new ServiceCollection();
        RegisterViewModels(services);
        RegisterViews(services);
        RegisterDiscordBot(services);

        return services.BuildServiceProvider();
    }

    private static void RegisterViews(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
    }

    private static void RegisterDiscordBot(IServiceCollection serviceCollection)
    {
        var botRunner = new Runner.Program();
        serviceCollection.AddSingleton(botRunner);
        serviceCollection.AddSingleton(botRunner.GetBotClient());
    }
}
