using Microsoft.Extensions.Configuration;
using Npgsql;
using Terminal.Gui;

namespace StationsScherm;

class Program
{
    static void Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .Build();
        Application.Init();
        Application.QuitKey = Key.Q.WithCtrl;
        NpgsqlConnection conn = new NpgsqlConnection(
            $"User ID={config["Database:User"]};Host={config["Database:Host"]};Database={config["Database:Name"]};Password={config["Database:Password"]}"
        );
        StationSelectionWindow stationSelectionWindow = new StationSelectionWindow(
            "Station Selection",
            conn
        );

        Application.Run(stationSelectionWindow);
        int stationsId = stationSelectionWindow.StationId;
        string stationName = stationSelectionWindow.StationName;
        StationScreen stationScreen = new StationScreen(conn, stationsId, stationName)
        {
            Title = "Station Screen",
        };
        Application.Run(stationScreen);

        Application.Shutdown();
    }
}
