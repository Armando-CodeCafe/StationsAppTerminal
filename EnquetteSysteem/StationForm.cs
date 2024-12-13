using System.ComponentModel;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;
using Terminal.Gui;

public class StationForm : Window
{
    int m_StationId;
    string m_StationName;
    const int MIN_SHORT_MESSAGE_LENGTH = 10;
    const int MAX_SHORT_MESSAGE_LENGTH = 100;
    NpgsqlConnection m_Connection;

    public StationForm(string title, int stationId, string stationName, NpgsqlConnection connection)
        : base()
    {
        ColorScheme = Colors.ColorSchemes["Menu"];
        Title = title;

        m_StationId = stationId;
        m_StationName = stationName;
        m_Connection = connection;
        Window w = new Window()
        {
            X = Pos.Center(),
            Y = Pos.Percent(10),
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            ColorScheme = this.ColorScheme,
        };
        Label titleLabel = new Label()
        {
            Title =
                $"Welcome to station {m_StationName}.\nPlease fill in the form below about your opinions\nand feedback about the station.",
            X = Pos.Center(),
            Y = Pos.Percent(5),
            TextAlignment = Alignment.Center,
            Width = Dim.Percent(60),
            Height = Dim.Auto(),
        };
        Label nameLabel = new Label()
        {
            Text = "Name: ",
            X = 0,
            Y = Pos.Bottom(titleLabel),
            Width = Dim.Auto(),
            Height = 1,
        };
        TextField nameField = new TextField()
        {
            Caption = "Name (empty for anonymous)",
            X = Pos.Right(nameLabel),
            Y = nameLabel.Y,
            Height = 1,
            Width = Dim.Fill(),
        };
        Label messageLabel = new Label()
        {
            Text = $"Short Message About Station {m_StationName}: ",
            X = 0,
            Y = Pos.Bottom(nameField) + 1,
            Width = Dim.Auto(),
            Height = 1,
        };

        TextView messageField = new TextView()
        {
            X = Pos.Right(messageLabel),
            Y = messageLabel.Y,
            Height = Dim.Auto(minimumContentDim: Dim.Percent(20)),
            Width = Dim.Fill(),
            WordWrap = true,
            AllowsTab = false,
        };
        Label messageLengthLabel = new Label()
        {
            Text =
                $"Length: {messageField.Text.Length}/{MAX_SHORT_MESSAGE_LENGTH}\n(minimum of {MIN_SHORT_MESSAGE_LENGTH})",
            X = messageField.X,
            Y = Pos.Bottom(messageField),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };
        Label feedbackLabel = new Label()
        {
            Title = "Please fill in any general feedback for 'De NS' you would like to tell us: ",
            X = 0,
            Y = Pos.Bottom(messageLengthLabel) + 1,
            Width = Dim.Auto(),
            Height = 1,
        };
        TextView feedbackField = new TextView
        {
            X = 0,
            Y = Pos.Bottom(feedbackLabel),
            Width = Dim.Fill(),
            Height = Dim.Auto(minimumContentDim: (Dim.Percent(20))),
            AllowsTab = false,
        };
        Button confirm = new Button()
        {
            Title = "Confirm",
            X = Pos.Center(),
            Y = Pos.Bottom(feedbackField),
        };
        confirm.Accept += (sender, args) =>
        {
            if (Submit(sender as Button, args, nameField, messageField, feedbackField))
            {
                MessageBox.Query("Success", "Feedback stored succesfully!", "Ok");
                nameField.Text = "";
                messageField.Text = "";
                feedbackField.Text = "";
            }
            else
            {
                MessageBox.Query(
                    "Error",
                    "Something went wrong, make sure to fill in all fields and check the character counts",
                    "Ok"
                );
            }
        };
        messageField.ContentsChanged += (sender, args) =>
        {
            messageLengthLabel.Text =
                $"Length: {messageField.Text.Length}/{MAX_SHORT_MESSAGE_LENGTH} (minimum of {MIN_SHORT_MESSAGE_LENGTH})";
            if (messageField.Text.Length > MAX_SHORT_MESSAGE_LENGTH)
            {
                messageField.Text = messageField.Text.Substring(0, MAX_SHORT_MESSAGE_LENGTH);
                messageField.MoveEnd();
            }
        };
        w.Add(
            nameLabel,
            nameField,
            messageLabel,
            messageField,
            messageLengthLabel,
            titleLabel,
            feedbackLabel,
            feedbackField,
            confirm
        );
        Add(w);
    }

    bool Submit(
        Button sender,
        HandledEventArgs args,
        TextField name,
        TextView message,
        TextView feedback
    )
    {
        name.Text = name.Text.Trim();
        message.Text = message.Text.Trim();
        feedback.Text = feedback.Text.Trim();
        if (message.Text.Trim().Length == 0)
        {
            return false;
        }
        m_Connection.Open();
        NpgsqlCommand command = new NpgsqlCommand(
            "INSERT INTO UserFeedback(name,message,feedback,date) VALUES (@name,@message,@feedback,@date)",
            m_Connection
        );
        command.Parameters.Add(
            new NpgsqlParameter("@name", name.Text.Length == 0 ? DBNull.Value : name.Text)
        );
        command.Parameters.Add(new NpgsqlParameter("@message", message.Text));
        command.Parameters.Add(new NpgsqlParameter("@feedback", feedback.Text));
        command.Parameters.Add(new NpgsqlParameter("@date", DateTime.Now));
        bool res = false;

        try
        {
            res = command.ExecuteNonQuery() != -1;
        }
        catch (NpgsqlException ne)
        {
            MessageBox.Query("Server error", $"Something went wrong:\n{ne.Message}", "Ok");
        }
        m_Connection.Close();
        return res;
    }
}
