using Npgsql;
using Terminal.Gui;

public class AdminLogin : Window
{
    NpgsqlConnection m_Connection;
    public Admin LoggedUser { get; set; }
    public AdminLogin(string title, NpgsqlConnection connection)
    {
        Title = title;
        m_Connection = connection;
        ColorScheme = Colors.ColorSchemes["Menu"];

        Window centerDiv = new Window()
        {
            Title = "Login",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(minimumContentDim: Dim.Percent(50)),
            Height = Dim.Auto(),
            ColorScheme = ColorScheme
        };
        Label emailLabel = new Label()
        {
            Text = "Email: ",
            X = Pos.Percent(20),
            Y = Pos.Percent(10),
            Width = Dim.Auto(),
            Height = Dim.Auto()
        };
        TextField emailField = new TextField()
        {
            Caption = "Email",
            X = Pos.Right(emailLabel),
            Y = Pos.Y(emailLabel),
            Width = Dim.Percent(60),
            Height = 1

        };
        Label passwordLabel = new Label()
        {
            Text = "Password: ",
            X = Pos.X(emailLabel),
            Y = Pos.Bottom(emailField) + 1,
            Width = Dim.Auto(),
            Height = Dim.Auto()
        };
        TextField passwordField = new TextField()
        {
            Caption = "Password",
            X = Pos.Right(passwordLabel),
            Y = Pos.Y(passwordLabel),
            Secret = true,
            Width = Dim.Percent(60),
            Height = 1

        };
        Button login = new Button()
        {
            Title = "Login",
            X = Pos.Center(),
            Y = Pos.Bottom(passwordField) + 1,
            Width = Dim.Auto(),
            Height = Dim.Auto()
        };
        login.Accept += (sender, args) =>
        {
            if (TryLogin(emailField, passwordField, out Admin user))
            {
                LoggedUser = user;
                Application.RequestStop();
            }
        };
        centerDiv.Add(emailField, emailLabel, passwordField, passwordLabel, login);
        Add(centerDiv);
    }
    bool TryLogin(TextField email, TextField password, out Admin user)
    {
        NpgsqlCommand command = m_Connection.CreateCommand();
        string emailString = email.Text.Trim().ToLower();
        string passwordString = Admin.HashString(password.Text.Trim());
        command.CommandText = "SELECT * Admins where email = @email and password = @password";
        command.Parameters.AddWithValue("@email", emailString);
        command.Parameters.AddWithValue("@password", passwordString);
        using (NpgsqlDataReader reader = command.ExecuteReader())
        {
            user = new Admin();
            while (reader.Read())
            {
                user.Email = reader.GetString(0);
                user.Password = reader.GetString(1);
                user.Name = reader.GetString(2);
                return true;
            }
            return false;
        }
    }
}