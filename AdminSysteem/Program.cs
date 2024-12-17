namespace AdminSysteem;

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Terminal.Gui;

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
        NpgsqlConnection connection = new NpgsqlConnection(
            $"User ID={config["Database:User"]};Host={config["Database:Host"]};Database={config["Database:Name"]};Password={config["Database:Password"]}"
        );
        CreateRootUser(connection);

        StationSelectionWindow stationSelectionWindow = new StationSelectionWindow(
            "Select Station",
            connection
        );
        Application.Run(stationSelectionWindow);
        int stationId = stationSelectionWindow.StationId;
        string stationName = stationSelectionWindow.StationName;
        AdminLogin adminLogin = new AdminLogin("Admin Login", connection);
        Application.Run(adminLogin);
        Admin? loggedIn = adminLogin.LoggedUser;
        if (loggedIn != null)
        {
            AdminDashboard adminDashboard = new AdminDashboard(
                "Admin Dashboard",
                connection,
                loggedIn,
                stationId
            )
            { };
            Application.Run(adminDashboard);
        }

        Application.Shutdown();
    }

    static void CreateRootUser(NpgsqlConnection connection)
    {
        connection.Open();
        string countQuery = "SELECT  COUNT(Id) FROM Admins";
        NpgsqlCommand command = new NpgsqlCommand(countQuery, connection);
        long? count = (long?)command.ExecuteScalar();
        if (count == null || count == 0)
        {
            Window registerWindow = new Window
            {
                ColorScheme = Colors.ColorSchemes["Menu"],
                Title = "Register first admin",
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = Dim.Percent(50),
                Height = Dim.Auto(minimumContentDim: Dim.Percent(30)),
            };
            TextField emailfield = new TextField
            {
                Caption = "Email",
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Auto() + 2,
            };
            TextField passwordfield = new TextField
            {
                Caption = "Password",
                Secret = true,
                X = 0,
                Y = Pos.Bottom(emailfield),
                Width = Dim.Fill(),
                Height = Dim.Auto() + 2,
            };
            TextField namefield = new TextField
            {
                Caption = "Full name",
                X = 0,
                Y = Pos.Bottom(passwordfield),
                Width = Dim.Fill(),
                Height = Dim.Auto() + 2,
            };
            Button registerButton = new Button
            {
                Title = "Register",
                X = Pos.Center(),
                Y = Pos.Bottom(namefield),
                Width = Dim.Auto(),
                Height = Dim.Auto(),
            };
            registerButton.Accept += (sender, args) =>
            {
                Regex emailRegex = new Regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$");
                string email = emailfield.Text.Trim().ToLower();
                Match emailMatch = emailRegex.Match(email);
                string password = passwordfield.Text.Trim();
                string name = namefield.Text.Trim();
                if (!emailMatch.Success || password == string.Empty || name == string.Empty)
                {
                    MessageBox.ErrorQuery(
                        "Register Error",
                        "Something went wrong, make sure the email is valid, and there is no empty boxes",
                        "Ok"
                    );
                    return;
                }
                password = Admin.HashString(password);
                string insertQuery =
                    "INSERT INTO Admins(email,password,name) VALUES(@email,@password,@name)";
                NpgsqlCommand command = new NpgsqlCommand(insertQuery, connection);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@password", password);
                command.Parameters.AddWithValue("@name", name);
                command.ExecuteNonQuery();
                Application.RequestStop();
            };
            registerWindow.Add(emailfield, passwordfield, namefield, registerButton);

            Application.Run(registerWindow);
        }
        connection.Close();
    }
}
