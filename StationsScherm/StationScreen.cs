using System.Data;
using System.Net.Http.Json;
using ColorHelper;
using Npgsql;
using OpenMeteo;
using Terminal.Gui;

public class StationScreen : Window
{
    NpgsqlConnection m_Connection;
    int m_StationId;
    string m_StationName;
    OpenMeteoClient m_MeteoClient = new OpenMeteoClient();

    HashSet<long> m_MessageSet = [];
    const int MaximumMessages = 6;

    public StationScreen(NpgsqlConnection connection, int stationId, string stationName)
    {
        m_Connection = connection;
        m_StationName = stationName;
        m_StationId = stationId;
        ColorScheme = Colors.ColorSchemes["Menu"];
        Func<string> GetDateString = () =>
        {
            DateTime now = DateTime.Now;
            return $"{now.ToString("dddd")} the {now.Day}{(now.Day % 10) switch {
            1=>"st",
            2=>"nd",
            3=>"rd",
            _=>"th"
            }} of {now.ToString("MMMM")}, {now.Year}";
        };
        Func<string> GetTimeString = () =>
        {
            return DateTime.Now.ToString("HH:mm:ss");
        };
        Func<string, string> GetIconForWeather = (string weatherCondition) =>
        {
            switch (weatherCondition)
            {
                case "0":
                    return "Clear Sky"; // ☀️
                case "1":
                    return "Partly Cloudy"; // 🌤️
                case "2":
                    return "Overcast"; // ☁️
                case "3":
                    return "Rain"; // 🌧️
                case "4":
                    return "Snow"; // ❄️
                case "5":
                    return "Thunderstorm"; // ⛈️
                default:
                    return "Rainbow"; // 🌈
            }
        };

        Func<string> GetWeatherString = () =>
        {
            Task<GeoInfo?> geotask = GeoInfo.GetCurrentGeoInfo();
            geotask.Wait();
            GeoInfo? geo = geotask.Result;
            WeatherForecast weather = m_MeteoClient.Query(
                new WeatherForecastOptions()
                {
                    Latitude = (float)geo.Lat,
                    Longitude = (float)geo.Lon,
                    Current = CurrentOptions.All,
                }
            );
            string weatherCondition = weather.Current.Weathercode.ToString(); // Assuming WeatherCode is available

            string weatherString =
                $"{weather.Current.Windspeed_10m}km/h, {weather.Current.Temperature} {weather.CurrentUnits.Temperature} {GetIconForWeather(weatherCondition)}";
            return weatherString;
        };
        Label dateLabel = new Label()
        {
            Text = GetDateString(),
            X = Pos.Center(),
            Y = Pos.Percent(10),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };
        Label timeLabel = new Label()
        {
            Text = GetTimeString(),
            X = Pos.AnchorEnd(),
            Y = 0,
            TextAlignment = Alignment.End,
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };
        Label weatherLabel = new Label()
        {
            Text = "",
            X = 0,
            Y = 0,
            Width = Dim.Auto(),
            Height = Dim.Auto(),
        };
        Label stationlabel = new Label()
        {
            Text = stationName,
            X = Pos.Center(),
            Y = Pos.Bottom(dateLabel),
            Width = Dim.Auto(),
            Height = 1,
        };
        FrameView utilities = new FrameView
        {
            Title = "Utilities",
            X = Pos.AnchorEnd(),
            Y = Pos.AnchorEnd(),
            Width = Dim.Percent(20),
            Height = Dim.Auto(),
            CanFocus = false,
            Enabled = false,
        };
        FrameView messagesView = new FrameView()
        {
            Title = "Messages",
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Percent(60),
            Height = Dim.Auto(),
            BorderStyle = LineStyle.None,
        };
        StationUtilities stationUtilities = GetStationUtilities();
        stationUtilities.GetList(utilities);
        Application.AddTimeout(
            TimeSpan.FromSeconds(1),
            () =>
            {
                timeLabel.Text = GetTimeString();

                return true;
            }
        );
        Application.AddTimeout(
            TimeSpan.FromSeconds(60),
            () =>
            {
                Task.Run(() =>
                {
                    string getweather = GetWeatherString();

                    weatherLabel.Text = getweather;
                });

                return true;
            }
        );
        Application.AddTimeout(
            TimeSpan.FromSeconds(3),
            () =>
            {
                Task.Run(() =>
                {
                    if (m_Connection.State == ConnectionState.Closed)
                        UpdateMessages(messagesView);
                });

                return true;
            }
        );
        Task.Run(() =>
        {
            string getweather = GetWeatherString();

            weatherLabel.Text = getweather;
        });
        Add(dateLabel, timeLabel, weatherLabel, stationlabel, utilities, messagesView);
    }

    void UpdateMessages(FrameView view)
    {
        List<Feedback> feedback = [];
        m_Connection.Open();
        NpgsqlCommand command = new NpgsqlCommand(
            @"SELECT t1.*
FROM Userfeedback AS t1
LEFT JOIN UserFeedbackAdministrated AS t2 
    ON t1.id = t2.feedbackId
WHERE t2.feedbackId IS NOT NULL AND t2.accepted IS TRUE AND t1.stationId = @stationid ORDER BY date DESC LIMIT 5 ;
",
            m_Connection
        );

        command.Parameters.AddWithValue("@stationid", m_StationId);
        using (NpgsqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                feedback.Add(
                    new Feedback
                    {
                        Id = reader.GetInt64(0),
                        Name = reader.GetString(1),
                        Message = reader.GetString(2),
                        FeedbackMessage = reader.GetString(3),
                        Date = reader.GetDateTime(4),
                        StationId = reader.GetInt64(5),
                        StationName = m_StationName,
                    }
                );
            }
        }

        if (
            m_MessageSet.SetEquals(feedback.Select(f => f.Id).ToArray())
            && m_MessageSet.Count == feedback.Count
        )
        {
            File.AppendAllText("log.txt", "NO NEW ENTRIES");
            m_Connection.Close();
            return;
        }
        File.AppendAllText("log.txt", "MESSAGE IDs\n");

        File.AppendAllText("log.txt", string.Join('\n', feedback.Select(f => f.Id).ToArray()));
        File.AppendAllText("log.txt", "HASHED IDs\n");

        File.AppendAllText("log.txt", string.Join('\n', m_MessageSet));
        List<View> subViews = view.Subviews.ToList();
        view.RemoveAll();
        subViews.ForEach(v => v.Dispose());

        FrameView? lastview = null;
        view.Add(
            feedback
                .Select(f =>
                {
                    FrameView messageFrame = new FrameView()
                    {
                        X = 0,
                        Y = lastview == null ? 0 : Pos.Bottom(lastview),
                        Width = Dim.Fill(),
                        Height = Dim.Auto(),
                        TextAlignment = Alignment.Center,
                        Title = $"{f.Name} at {f.Date.ToString("yyyy-MM-dd HH:mm:ss")} said:",
                    };
                    Label message = new Label()
                    {
                        Text = f.Message,
                        X = 0,
                        Y = 0,
                        TextAlignment = Alignment.Center,
                        Width = Dim.Fill(),
                        Height = Dim.Auto(),
                    };
                    messageFrame.Add(message);
                    lastview = messageFrame;
                    return messageFrame;
                })
                .ToArray()
        );
        m_MessageSet = feedback.Select(f => f.Id).ToHashSet();
        m_Connection.Close();
    }

    StationUtilities GetStationUtilities()
    {
        m_Connection.Open();
        NpgsqlCommand command = new NpgsqlCommand(
            "SELECT * FROM netherlands_train_stations WHERE id = @id",
            m_Connection
        );
        command.Parameters.AddWithValue("@id", m_StationId);
        using (NpgsqlDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                StationUtilities utilities = new StationUtilities
                {
                    Lift = reader.GetBoolean(2),
                    WheelChairAccesible = reader.GetBoolean(3),
                    Toilet = reader.GetBoolean(4),
                    Kiosk = reader.GetBoolean(5),
                };
                m_Connection.Close();

                return utilities;
            }
        }
        m_Connection.Close();
        return new StationUtilities();
    }
}

public class GeoInfo
{
    public string Query { get; set; }
    public string Status { get; set; }
    public string Country { get; set; }
    public string CountryCode { get; set; }
    public string Region { get; set; }
    public string RegionName { get; set; }
    public string City { get; set; }
    public string Zip { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Timezone { get; set; }
    public string Isp { get; set; }
    public string Org { get; set; }
    public string As { get; set; }

    public static async Task<GeoInfo?> GetCurrentGeoInfo()
    {
        HttpClient httpClient = new HttpClient();
        HttpResponseMessage response = await httpClient.GetAsync("http://ip-api.com/json");

        GeoInfo? geoInfo = await response.Content.ReadFromJsonAsync<GeoInfo>();
        return geoInfo;
    }
}

public class StationUtilities
{
    public bool Lift { get; set; }
    public bool WheelChairAccesible { get; set; }
    public bool Toilet { get; set; }
    public bool Kiosk { get; set; }

    public void GetList(FrameView baseView)
    {
        Dictionary<string, bool> values = new Dictionary<string, bool>
        {
            { "Lift", Lift },
            { "Wheelchair", WheelChairAccesible },
            { "Toilet", Toilet },
            { "Kiosk", Kiosk },
        };
        Label lastlabel = null;
        foreach (KeyValuePair<string, bool> val in values)
        {
            baseView.Add(
                lastlabel = new Label
                {
                    Text = val.Key,
                    X = 0,
                    Y = lastlabel == null ? 0 : Pos.Bottom(lastlabel),
                    Width = Dim.Auto(minimumContentDim: Dim.Percent(50)),
                    Height = 1,
                }
            );
            baseView.Add(
                new Label()
                {
                    Text = val.Value ? "✅" : "❌",
                    Enabled = false,
                    CanFocus = false,
                    X = Pos.Right(lastlabel),
                    Y = Pos.Y(lastlabel),
                    ColorScheme = new ColorScheme
                    {
                        Normal = Application.Driver.MakeColor(
                            val.Value ? Color.Green : Color.Red,
                            Color.Black
                        ),
                        Disabled = Application.Driver.MakeColor(
                            val.Value ? Color.Green : Color.Red,
                            Color.Black
                        ),

                        Focus = Application.Driver.MakeColor(
                            val.Value ? Color.Green : Color.Red,
                            Color.Black
                        ),
                    },
                }
            );
        }
        ;
    }
}
