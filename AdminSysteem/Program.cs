namespace AdminSysteem;

using Microsoft.Extensions.Configuration;
using Npgsql;
using Terminal.Gui;

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
        NpgsqlConnection connection = new NpgsqlConnection($"User ID={config["Database:User"]};Host={config["Database:Host"]};Database={config["Database:Name"]};Password={config["Database:Password"]}");
        StationSelectionWindow stationSelectionWindow = new StationSelectionWindow("Select Station", connection);
        Application.Run(stationSelectionWindow);
        int stationId = stationSelectionWindow.StationId;
        string stationName = stationSelectionWindow.StationName;
        AdminLogin adminLogin = new AdminLogin("Admin Login", connection);
        Application.Run(adminLogin);

        Application.Shutdown();
    }
}
