using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Alchemy;
using Alchemy.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Media;
using System.Timers;

namespace ActivityClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Settings sets;
        WebSocketClient wsClient;

        public delegate void TimeChangeHandler(int currentime);
        public event TimeChangeHandler OnTimeChange;

        bool isInFullScreen = false;

        string[] teams = new string[6];

        SoundPlayer clockSound = new SoundPlayer(Properties.Resources.clock);
        SoundPlayer bammSound = new SoundPlayer(Properties.Resources.bamm);
        SoundPlayer startSound = new SoundPlayer(Properties.Resources.starting);
        SoundPlayer endSound = new SoundPlayer(Properties.Resources.end);

        Timer blinker;

        public MainWindow(Settings sets)
        {
            InitializeComponent();
    
            blinker = new Timer();
            blinker.Interval = 250;
            blinker.Elapsed += blinker_Elapsed;

            this.sets = sets;
            this.OnTimeChange += MainWindow_OnTimeChange;

            if (sets.FontSize == "large")
            {
                txtTimer.FontSize = 450;
                txtTeam.FontSize = 100;
            }
            else if (sets.FontSize == "medium")
            {
                txtTimer.FontSize = 300;
                txtTeam.FontSize = 70;
            }
            else
            {
                txtTimer.FontSize = 200;
                txtTeam.FontSize = 50;
            }

            wsClient = new WebSocketClient("ws://" + sets.IPAddress + ":8000/channel")
            {
                OnConnected = OnConnect,
                OnDisconnect = OnDisconnect,
                OnReceive = OnReceive
            };

            try {
               wsClient.Connect();
            }
            catch (Exception)
            {

            }
        }

        void blinker_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (sets.AnimationsEnabled)
                {
                    if (txtTimer.Visibility == Visibility.Visible)
                    {
                        txtTimer.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        txtTimer.Visibility = Visibility.Visible;
                    }
                }
            }));
        }

        void blinkerReset()
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                txtTimer.Visibility = Visibility.Visible;
            }));
        }

        public void MainWindow_OnTimeChange(int value)
        {
             if (value < 40 && value > 30)
             {
                 if (!blinker.Enabled)
                 {
                     blinker.Start();
                     if (sets.MusicEffectsEnabled)
                     {
                         clockSound.PlayLooping();
                     }
                 };
             }
             if (value == 30)
             {
                 if (blinker.Enabled)
                 {
                     blinker.Stop();
                     blinkerReset();
                     clockSound.Stop();
                     if (sets.MusicEffectsEnabled)
                     {
                         bammSound.Play();
                     }
                 }
             }
             if (value > 9)
             {
                 this.Dispatcher.Invoke((Action)(() =>
                 {
                    txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                 }));
             }
             if (value < 10 && value > 0)
             {
                 if (!blinker.Enabled)
                 {
                     this.Dispatcher.Invoke((Action)(() =>
                     {
                         if (sets.AnimationsEnabled)
                         {
                             txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 30, 30));
                         }
                     }));
                     blinker.Start();
                     if (sets.MusicEffectsEnabled)
                     {
                         clockSound.PlayLooping();
                     }
                 }
             }
             if (value == 0)
             {
                 if (blinker.Enabled)
                 {
                     this.Dispatcher.Invoke((Action)(() =>
                     {
                         txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                     }));
                     blinker.Stop();
                     blinkerReset();
                     clockSound.Stop();
                     if (sets.MusicEffectsEnabled)
                     {
                         endSound.Play();
                     }
                 }
             }
            txtTimer.Text = TimeFormatter(value);
        }

        public string TimeFormatter(int value)
        {
            TimeSpan duration = new TimeSpan(0, 0, value);
            return duration.ToString(@"mm\:ss");
        }

        private void OnDisconnect(UserContext context)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                txtStatus.Text = "Nincs kapcsolat a szerverrel, újracsatlakozás...";
            }));
        }

        private void OnConnect(UserContext context)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                txtStatus.Text = "Csatlakozva a szerverhez";
            }));
        }

        private void OnReceive(UserContext context)
        {
            try
            {
                var json = context.DataFrame.ToString();

                dynamic obj = JsonConvert.DeserializeObject(json);

                switch ((string)obj.action)
                {
                    case "refreshTime":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            OnTimeChange((int)obj.value);
                        }));
                        break;
                    case "init":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            int time = Convert.ToInt32(JObject.Parse(json)["value"]["timer"].ToString());
                            txtTimer.Text = TimeFormatter(time);

                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                if (sets.AnimationsEnabled && time < 9 && time > 0)
                                {
                                    txtTimer.Foreground = new SolidColorBrush(Color.FromRgb(255, 30, 30));
                                }
                            }));

                            List<String> teamsList = JObject.Parse(json)["value"]["teams"].ToObject<List<String>>();
                            teams = teamsList.ToArray();

                            int buttonStatus = Convert.ToInt32(JObject.Parse(json)["value"]["buttonStatus"].ToString());
                            if (buttonStatus != 0)
                            {
                                txtTeam.Text = ("Rablás: " + teams[(buttonStatus - 1)] + " csapat (" + buttonStatus + ")").ToUpper();
                                Thickness m = txtTimer.Margin;
                                m.Bottom = 200;
                                txtTimer.Margin = m;
                            }
                        }));
                        break;
                    case "teamsEdited":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            List<String> teamsList = JObject.Parse(json)["value"].ToObject<List<String>>();
                            teams = teamsList.ToArray();
                        }));
                        break;
                    case "buttonPress":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            if (sets.TeamDisplayEnabled)
                            {
                                if ((int)obj.value != 0)
                                {
                                    txtTeam.Text = ("Rablás: " + teams[(((int)obj.value) - 1)] + " csapat (" + (int)obj.value + ")").ToUpper();
                                    Thickness m = txtTimer.Margin;
                                    m.Bottom = 200;
                                    txtTimer.Margin = m;
                                }
                            }
                        }));
                        break;
                    case "buttonReset":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            if (sets.TeamDisplayEnabled)
                            {
                                txtTeam.Text = "";
                                Thickness m = txtTimer.Margin;
                                m.Bottom = 0;
                                txtTimer.Margin = m;
                            }
                        }));
                        break;
                    case "timerStatus":
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            if (sets.MusicEffectsEnabled)
                            {
                                if ((bool)obj.value)
                                {
                                    startSound.Play();
                                }
                                else
                                {
                                    blinker.Stop();
                                    blinkerReset();
                                    bammSound.Play();
                                }
                                
                            }
                        }));
                        break;
                }
            }
            catch (Exception) {}
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    this.Close();
                    break;
                case Key.Enter:
                    changeWindowSize();
                    break;
                case Key.Space:
                    wsClient.Send("{ action: 'startstop' }");
                    break;
                case Key.Back:
                    wsClient.Send("{ action: 'reset' }");
                    break;
                case Key.D1:
                    wsClient.Send("{ action: 'modeSelect', value: 'NORMAL' }");
                    break;
                case Key.D2:
                    wsClient.Send("{ action: 'modeSelect', value: 'HALFROBBER' }");
                    break;
                case Key.D3:
                    wsClient.Send("{ action: 'modeSelect', value: 'FULLROBBER' }");
                    break;
                case Key.D4:
                    wsClient.Send("{ action: 'modeSelect', value: 'EXTRA' }");
                    break;
            }
        }

        public void changeWindowSize()
        {
            if (isInFullScreen)
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.ToolWindow;
                txtInfo.Visibility = Visibility.Visible;
                txtStatus.Visibility = Visibility.Visible;
                isInFullScreen = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
                txtInfo.Visibility = Visibility.Hidden;
                txtStatus.Visibility = Visibility.Hidden;
                isInFullScreen = true;
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            changeWindowSize();
        }
        
    }
}
