using Npgsql;
using Terminal.Gui;

namespace EnquetteSysteem;

class Program
{
    static void Main(string[] args)
    {
        Application.Init();
        Application.QuitKey = Key.Q.WithCtrl;
        NpgsqlConnection conn = new NpgsqlConnection(
            "User ID=postgres;Host=localhost;Database=StationsApp;Password=postgres"
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
