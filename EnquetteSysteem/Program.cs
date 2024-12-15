using Npgsql;
using Terminal.Gui;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace EnquetteSysteem;

class Program
{
    static void Main(string[] args)
    {
        var config =
    new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", false)
        .Build();
        Application.Init();
        Application.QuitKey = Key.Q.WithCtrl;
        File.WriteAllLines("log.txt", config.GetChildren().Select(c => $"{c.Key}: {c.Value}").ToArray());
        NpgsqlConnection conn = new NpgsqlConnection(
            $"User ID={config["Database:User"]};Host={config["Database:Host"]};Database={config["Database:Name"]};Password={config["Database:Password"]}"
        );
        StationSelectionWindow stationSelectionWindow = new StationSelectionWindow(
            "Station Selection",
            conn
        );

        Application.Run(stationSelectionWindow);
        StationForm form = new StationForm(
            "NS Enquette",
            stationSelectionWindow.StationId,
            stationSelectionWindow.StationName,
            conn
        );
        Application.Run(form);
        Application.Shutdown();
    }
}
