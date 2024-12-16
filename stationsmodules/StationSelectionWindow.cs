using System.Collections;
using System.Collections.Specialized;
using Npgsql;
using Terminal.Gui;

public class StationSelectionWindow : Window
{
    NpgsqlConnection m_Conn;
    Dictionary<int, string> m_Stations = [];
    public int StationId { get; private set; }
    public string StationName { get; private set; }

    public StationSelectionWindow(string title, NpgsqlConnection connection)
        : base()
    {
        ColorScheme = Colors.ColorSchemes["Menu"];

        Title = title;
        m_Conn = connection;
        Width = Dim.Fill();
        Height = Dim.Fill();
        m_Conn.Open();
        NpgsqlCommand cmd = new NpgsqlCommand(
            "SELECT id, name FROM netherlands_train_stations",
            m_Conn
        );
        using (NpgsqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                m_Stations.Add(reader.GetInt32(0), reader.GetString(1));
            }
        }
        DictionaryListDataSource<int, string> dictionaryListDataSource =
            new DictionaryListDataSource<int, string>(m_Stations);
        Label label = new Label()
        {
            Title = "Please pick a station for this system to run on:",
            X = Pos.Center(),
            Y = Pos.Percent(40),
            Height = 1,
        };
        ComboBox dropdown = new ComboBox()
        {
            Text = m_Stations.ToArray()[0].Value.ToString(),
            X = Pos.Center(),
            Y = Pos.Bottom(label),
            Source = dictionaryListDataSource,
            Width = Dim.Percent(30),
            Height = Dim.Fill(),
            ReadOnly = true,
            CanFocus = true,
        };
        Button confirm = new Button()
        {
            Text = "Confirm",
            X = Pos.Right(dropdown),
            Y = dropdown.Y,
        };
        confirm.Accept += (sender, args) =>
        {
            if (dropdown.SelectedItem != null)
            {
                try
                {
                    StationId = m_Stations.Keys.ToArray()[dropdown.SelectedItem];
                    StationName = m_Stations[StationId];
                    RequestStop();
                }
                catch (IndexOutOfRangeException)
                {
                    MessageBox.ErrorQuery(
                        "No Station Selected",
                        "Make sure to press Enter on the station you want to select",
                        "Ok"
                    );
                }
            }
        };
        dropdown.KeyDown += (sender, keyEvent) =>
        {
            if (keyEvent == Key.Enter || keyEvent == Key.Space)
            {
                dropdown.Expand(); // Opens the dropdown
                keyEvent.Handled = true; // Prevent default handling
            }
        };

        m_Conn.Close();
        Add(label, dropdown, confirm);
    }
}

public class DictionaryListDataSource<Tkey, Tvalue> : IListDataSource
    where Tkey : notnull
{
    public int Count => keyValuePairs.Count;

    public int Length => keyValuePairs.Count;

    public bool SuspendCollectionChangedEvent { get; set; }

    Dictionary<Tkey, (Tvalue value, bool marked)> keyValuePairs = [];

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public DictionaryListDataSource(Dictionary<Tkey, Tvalue> dictionary)
    {
        keyValuePairs = dictionary
            .Select(kvp => new KeyValuePair<Tkey, (Tvalue value, bool marked)>(
                kvp.Key,
                (kvp.Value, false)
            ))
            .ToDictionary(k => k.Key, k => k.Value);
    }

    public bool IsMarked(int item)
    {
        (Tvalue value, bool marked) = keyValuePairs.ToList()[item].Value;
        return marked;
    }

    public void Render(
        ListView container,
        ConsoleDriver driver,
        bool selected,
        int item,
        int col,
        int line,
        int width,
        int start = 0
    )
    {
        Tkey key = keyValuePairs.ToList()[item].Key;
        Tvalue value = keyValuePairs[key].value;
        string displayText = $"{key}: {value}";
        string paddedText = displayText.PadRight(width);

        if (selected)
        {
            driver.SetAttribute(container.ColorScheme.Focus);
        }
        else
        {
            driver.SetAttribute(container.ColorScheme.Normal);
        }

        driver.AddStr(paddedText.Substring(0, Math.Min(width, paddedText.Length)));
    }

    public void SetMark(int item, bool value)
    {
        KeyValuePair<Tkey, (Tvalue, bool)> kvp = keyValuePairs.ToList()[item];
        Tkey key = kvp.Key;
        Tvalue kvpvalue = kvp.Value.Item1;
        keyValuePairs[key] = (kvpvalue, value);
    }

    public IList ToList()
    {
        return keyValuePairs.Values.Select(t => t.value).ToArray();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
