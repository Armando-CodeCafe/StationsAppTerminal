using System.Data;
using Npgsql;
using Terminal.Gui;

public class FeedbackManagement : FrameView
{
    NpgsqlConnection m_Connection;
    Feedback m_NextFeedback;
    Admin m_LoggedIn;

    public FeedbackManagement(string title, NpgsqlConnection connection, Admin loggedin)
    {
        m_LoggedIn = loggedin;
        Title = title;
        m_Connection = connection;
        ColorScheme = Colors.ColorSchemes["Menu"];
        object timeout = null;
        Button backButton = new Button()
        {
            Text = "Back",
            X = 0,
            Y = 0,
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };

        Label nextMessageLabel = new Label
        {
            X = 0,
            Y = Pos.Bottom(backButton),
            Width = Dim.Fill(),
            Height = Dim.Auto(minimumContentDim: Dim.Absolute(1)),
        };
        Label messageLabel = new Label
        {
            Text = "Message:",
            X = 0,
            Y = Pos.Bottom(nextMessageLabel),
            Width = Dim.Fill(),
            Height = Dim.Auto(),
        };
        TextField messageField = new TextField
        {
            ReadOnly = true,
            Caption = "Message",
            X = 0,
            Y = Pos.Bottom(messageLabel),
            Width = Dim.Fill(),
            TabStop = TabBehavior.NoStop,
            Height = Dim.Auto(minimumContentDim: Dim.Absolute(1)),
        };
        Label feedbackLabel = new Label
        {
            Text = "Feedback:",
            X = 0,
            Y = Pos.Bottom(messageField),
            Width = Dim.Fill(),
            Height = Dim.Auto(),
        };
        TextField feedbackField = new TextField
        {
            ReadOnly = true,
            Caption = "Feedback",
            X = 0,
            TabStop = TabBehavior.NoStop,
            Y = Pos.Bottom(feedbackLabel),
            Width = Dim.Fill(),

            Height = Dim.Auto(minimumContentDim: Dim.Absolute(1)),
        };
        Button denyButton = new Button()
        {
            Text = "Denied",
            X = Pos.Center(),
            Y = Pos.Bottom(feedbackField),
            Width = Dim.Auto(),
            Height = 1,
        };
        Button acceptButton = new Button()
        {
            Text = "Accept",
            X = Pos.Right(denyButton),
            Y = Pos.Y(denyButton),
            Width = Dim.Auto(),
            Height = 1,
        };
        backButton.Accept += (sender, args) =>
        {
            SuperView.AdvanceFocus(NavigationDirection.Forward, TabBehavior.TabGroup);
            Application.RemoveTimeout(timeout);
        };
        Add(
            nextMessageLabel,
            messageField,
            messageLabel,
            feedbackField,
            feedbackLabel,
            denyButton,
            acceptButton,
            backButton
        );
        Action GetFeedback = () =>
        {
            File.WriteAllText("log.txt", "Getting feedback " + DateTime.Now.ToString());

            if (m_Connection.State == ConnectionState.Open)
            {
                return;
            }
            if (TryGetNextFeedback(out Feedback? feedback))
            {
                messageField.Enabled = true;
                feedbackField.Enabled = true;
                denyButton.Enabled = true;
                acceptButton.Enabled = true;
                m_NextFeedback = feedback;
                nextMessageLabel.Text =
                    $"Latest feedback about {feedback.StationName} from {feedback.Name} at {feedback.Date.ToString("yyyy-mm-dd HH:mm:ss")}";
                messageField.Text = feedback.Message;
                feedbackField.Text = feedback.FeedbackMessage;
            }
            else
            {
                nextMessageLabel.Text = "Currently no feedback to administrate";
                messageField.Enabled = false;
                feedbackField.Enabled = false;
                denyButton.Enabled = false;
                acceptButton.Enabled = false;
                messageField.Text = "";
                feedbackField.Text = "";
            }
        };
        acceptButton.Accept += (sender, args) =>
        {
            AdministrateFeedback(m_NextFeedback, true);
            m_NextFeedback = null;
            GetFeedback.Invoke();
        };
        denyButton.Accept += (sender, args) =>
        {
            AdministrateFeedback(m_NextFeedback, false);
            m_NextFeedback = null;
            GetFeedback.Invoke();
        };
        Added += (sender, args) => GetFeedback();

        timeout = Application.AddTimeout(
            TimeSpan.FromMilliseconds(3000),
            () =>
            {
                GetFeedback();
                return true;
            }
        );
    }

    void AdministrateFeedback(Feedback feedback, bool accepted)
    {
        m_Connection.Open();
        string insertQuery =
            "INSERT INTO UserFeedbackAdministrated(feedbackid,adminid,dateadministrated,accepted) VALUES (@feedbackid,@adminid,@dateadministrated,@accepted) ";
        NpgsqlCommand insertCommand = new NpgsqlCommand(insertQuery, m_Connection);
        insertCommand.Parameters.AddWithValue("@feedbackid", feedback.Id);
        insertCommand.Parameters.AddWithValue("@adminid", m_LoggedIn.Id);
        insertCommand.Parameters.AddWithValue("@dateadministrated", DateTime.Now);
        insertCommand.Parameters.AddWithValue("@accepted", accepted);
        insertCommand.ExecuteNonQuery();
        m_Connection.Close();
        MessageBox.Query("Success", "Succesfully rated feedback", "Ok");
    }

    bool TryGetNextFeedback(out Feedback? nextFeedback)
    {
        m_Connection.Open();
        string countQuery =
            "SELECT COUNT(UserFeedback.Id) FROM UserFeedback LEFT JOIN UserFeedbackAdministrated ON UserFeedback.Id = UserFeedbackAdministrated.FeedbackId WHERE UserFeedbackAdministrated.FeedbackId IS NULL";
        NpgsqlCommand countCommand = new NpgsqlCommand(countQuery, m_Connection);
        long? count = (long?)countCommand.ExecuteScalar();
        if (count == 0)
        {
            nextFeedback = null;
            m_Connection.Close();
            return false;
        }
        nextFeedback = new Feedback();
        string nextFeedbackQuery =
            "SELECT UserFeedback.id, UserFeedback.name,UserFeedback.message,UserFeedback.feedback,UserFeedback.date,UserFeedback.StationId,netherlands_train_stations.name as StationName FROM UserFeedback LEFT JOIN UserFeedbackAdministrated ON UserFeedback.Id = UserFeedbackAdministrated.FeedbackId INNER JOIN netherlands_train_stations ON UserFeedback.StationId = netherlands_train_stations.Id WHERE UserFeedbackAdministrated.FeedbackId IS NULL LIMIT 1 ";
        NpgsqlCommand nextFeedbackCommand = new NpgsqlCommand(nextFeedbackQuery, m_Connection);
        NpgsqlDataReader reader = nextFeedbackCommand.ExecuteReader();
        while (reader.Read())
        {
            nextFeedback.Id = reader.GetInt64(0);
            nextFeedback.Name = reader.GetString(1);
            nextFeedback.Message = reader.GetString(2);
            nextFeedback.FeedbackMessage = reader.GetString(3);
            nextFeedback.Date = reader.GetDateTime(4);
            nextFeedback.StationId = reader.GetInt64(5);
            nextFeedback.StationName = reader.GetString(6);
        }
        m_Connection.Close();
        return true;
    }
}
