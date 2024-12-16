using Npgsql;
using Terminal.Gui;

public class AdminDashboard : Window
{
    NpgsqlConnection m_Connection;
    FrameView m_MainWindow;
    Admin m_LoggedIn;

    public AdminDashboard(string title, NpgsqlConnection connection, Admin admin)
    {
        m_LoggedIn = admin;
        ColorScheme = Colors.ColorSchemes["Menu"];
        Title = title;
        m_Connection = connection;
        FrameView treeview = new FrameView
        {
            Title = "Navigation",
            ColorScheme = this.ColorScheme,
            X = 0,
            Y = 0,
            Width = Dim.Percent(25),
            Height = Dim.Fill() - 1,
        };

        TreeView navbar = new TreeView()
        {
            ColorScheme = this.ColorScheme,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        //users
        TreeNode userManagement = new TreeNode("Users");
        TreeNode userCreate = new TreeNode("Create User");
        userManagement.Children.Add(userCreate);
        navbar.AddObject(userManagement);
        //feedback administration
        TreeNode administration = new TreeNode("Administration");
        TreeNode feedbackManagement = new TreeNode("Feedback Management");

        administration.Children.Add(feedbackManagement);
        TreeNode feedbackOverview = new TreeNode("Feedback Overview");
        administration.Children.Add(feedbackOverview);
        navbar.AddObject(administration);
        Action<FrameView> SetMain = (w) =>
        {
            w.ColorScheme = this.ColorScheme;
            w.X = Pos.Right(treeview);
            w.Y = 0;
            w.Width = Dim.Fill();
            w.Height = Dim.Fill();
        };
        navbar.Accept += (sender, args) =>
        {
            TreeNode node = navbar.SelectedObject as TreeNode;
            // Remove(m_MainWindow);

            switch (node)
            {
                case TreeNode feedbackManagement:
                    m_MainWindow = new FeedbackManagement("Feedback", m_Connection, m_LoggedIn);
                    Add(m_MainWindow);

                    break;
            }
            SetMain(m_MainWindow);
            m_MainWindow.SetFocus();
        };
        //mainview
        FrameView main = new FrameView();
        SetMain(main);
        main.Title = "Main";
        m_MainWindow = main;
        treeview.Add(navbar);

        Add(treeview, m_MainWindow);
    }
}
