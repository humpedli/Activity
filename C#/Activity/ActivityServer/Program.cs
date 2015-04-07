using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alchemy;
using Alchemy.Classes;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.Ports;
using System.Timers;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ActivityServer
{
    class Program
    {
        WebSocketServer wsServer;
        ConcurrentDictionary<Client, string> clients = new ConcurrentDictionary<Client, string>();

        SerialPort sp;
        string comPort;
        int baudRate;

        Timer secondTimer;
        int currentTime = 90;

        string gameMode = "NORMAL";
        bool buttonsEnabledStatus = false;
        int lastPressedButton = 0;

        ConcurrentDictionary<int, Team> teams = new ConcurrentDictionary<int, Team>();

        static void Main(string[] args)
        {
            Program p = new Program();
            p.OpenTeamList(); 
            p.WebSocketServer();
            p.InitTimer();
            p.InitSerial();

            p.WriteToConsole("Használható billentyűparancsok:", ConsoleColor.Gray);
            p.WriteToConsole("-------------------------------", ConsoleColor.Gray);
            p.WriteToConsole("ESC - Kilépés a programból", ConsoleColor.Gray);
            p.WriteToConsole("SPACE - Start/stop", ConsoleColor.Gray);
            p.WriteToConsole("BACKSPACE - Reset", ConsoleColor.Gray);
            p.WriteToConsole("1, 2, 3, 4 - Játékmód beállítás (Normál, Fél-rabló, Rabló, Extra)", ConsoleColor.Gray);

            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey();
                switch (keyinfo.Key)
                {
                    case ConsoleKey.Spacebar:
                        p.StartStopTimer();
                        break;
                    case ConsoleKey.Backspace:
                        p.ResetTimer();
                        break;
                    case ConsoleKey.D1:
                        p.modeSelect("NORMAL");
                        break;
                    case ConsoleKey.D2:
                        p.modeSelect("HALFROBBER");
                        break;
                    case ConsoleKey.D3:
                        p.modeSelect("FULLROBBER");
                        break;
                    case ConsoleKey.D4:
                        p.modeSelect("EXTRA");
                        break;
                }
            }
            while (keyinfo.Key != ConsoleKey.Escape);

            p.CloseConnections();
        }

        public void WebSocketServer()
        {
            WriteToConsole("Szerver inicializálása", ConsoleColor.White);

            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            wsServer = new WebSocketServer(8000, IPAddress.Any)
            {
                OnReceive = OnReceive,
                OnSend = OnSend,
                OnConnected = OnConnect,
                OnDisconnect = OnDisconnect,
                TimeOut = new TimeSpan(0, 5, 0),
                FlashAccessPolicyEnabled = true
            };

            wsServer.Start();

            WriteToConsole("Szerver elindítva a " + LocalIPAddress() + ":8000 címen", ConsoleColor.White);
            WriteToConsole("Várakozás a csatlakozásra...", ConsoleColor.White);
        }

        public void SendInitData()
        {
            string[] tms = new string[6];

            foreach (var item in teams)
            {
                tms[item.Key] = item.Value.Name;
            }

            var obj = new
            {
                action = "init",
                value = new {
                    timer = currentTime,
                    gameMode = gameMode,
                    buttonStatus = lastPressedButton,
                    teams = tms,
                    timerStatus = secondTimer.Enabled
                }
            };

            Send(JsonConvert.SerializeObject(obj));
        }

        public void OnConnect(UserContext context)
        {  
            WriteToConsole("Egy kliens csatlakozott " + context.ClientAddress.ToString(), ConsoleColor.Green);
            clients.TryAdd(new Client { Context = context }, String.Empty);
            SendInitData();
        }

        public void OnDisconnect(UserContext context)
        {
            WriteToConsole("Egy kliens lecsatlakozott " + context.ClientAddress.ToString(), ConsoleColor.Red);

            string trash;
            try
            {
                var client = clients.Keys.Where(c => c.Context.ClientAddress == context.ClientAddress).Single();
                clients.TryRemove(client, out trash);
            }
            catch (Exception) { }
        }

        public void OnReceive(UserContext context)
        {
            try
            {
                var json = context.DataFrame.ToString();

                dynamic obj = JsonConvert.DeserializeObject(json);

                WriteToConsole("Beérkezett parancs: " + Convert.ToString((string)obj.action).ToUpper(), ConsoleColor.Cyan);

                switch ((string)obj.action)
                {
                    case "start":
                        StartTimer();
                        break;
                    case "stop":
                        StopTimer();
                        break;
                    case "startstop":
                        StartStopTimer();
                        break;
                    case "reset":
                        ResetTimer();
                        break;
                    case "modeSelect":
                        modeSelect((string)obj.value);
                        break;
                    case "editTeams":
                        editTeams(json);
                        break;

                }
                
            }
            catch (Exception) {}
        }

        public void OnSend(UserContext context)
        {
            //Console.WriteLine("On send:" + context);
        }

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        public void InitSerial()
        {
            try
            {
                FileStream fs = new FileStream("config.txt", FileMode.Open);
                StreamReader rs = new StreamReader(fs);
                string s = rs.ReadToEnd();
                fs.Close();

                string[] fileLines = s.Split(new char[] { '\n' });
                for (int i = 0; i < fileLines.Length; i++)
                {
                    string[] com = fileLines[i].Split(new string[] { "ComPort=" }, StringSplitOptions.None);
                    if (com.Length > 1)
                    {
                        comPort = com[1].Trim();
                    }
                    string[] baud = fileLines[i].Split(new string[] { "BaudRate=" }, StringSplitOptions.None);
                    if (baud.Length > 1)
                    {
                        baudRate = Convert.ToInt32(baud[1].Trim());
                    }
                }
                sp = new System.IO.Ports.SerialPort(comPort, baudRate, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
                sp.Open();
                sp.DataReceived += SerialOnReceive;
                WriteToConsole("Gombok inicializálása", ConsoleColor.White);
                ResetButtons();
            }
            catch (Exception)
            {
                WriteToConsole("Nem sikerült inicializálni a gombokat, ellenőrizd a csatlakozásokat és a konfigurációs fájlt!", ConsoleColor.Red);
            }
        }

        private void SerialOnReceive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                StopTimer();
                lastPressedButton = Convert.ToInt32(sp.ReadExisting());
                Send(JsonConvert.SerializeObject(new { action = "buttonPress", value = lastPressedButton }));
                WriteToConsole("Gombnyomás érzékelve: " + lastPressedButton, ConsoleColor.Magenta);
            }
            catch (Exception)
            {
                WriteToConsole("Érvénytelen adat érkezett a gomboktól!", ConsoleColor.Red);
            }
        }

        public void WriteToConsole(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("\r[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message);
            Console.ResetColor();
        }

        public void InitTimer()
        {
            secondTimer = new Timer();
            secondTimer.Interval = 1000;
            secondTimer.Elapsed += secondTimer_Elapsed;
        }

        void secondTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            currentTime--;

            if (currentTime == 30 && gameMode != "EXTRA")
            {
                StopTimer();    
            }

            if (currentTime == 0)
            {
                StopTimer();
            }

            Send(JsonConvert.SerializeObject(new { action = "refreshTime", value = currentTime }));
            if (secondTimer.Enabled)
                {
                Console.Write("\rAz időzítő megy: {0}", currentTime);
            }
        }

        public void DisableButtons()
        {
            try
            {
                sp.Write("d");
            }
            catch (Exception) { }
            buttonsEnabledStatus = false;
            Send(JsonConvert.SerializeObject(new { action = "buttonsEnabledStatus", value = buttonsEnabledStatus }));
            WriteToConsole("A gombok le lettek tiltva", ConsoleColor.DarkMagenta);
        }

        public void EnableButtons()
        {
            try
            {
                sp.Write("e");
            }
            catch (Exception) { }
            buttonsEnabledStatus = true;
            Send(JsonConvert.SerializeObject(new { action = "buttonsEnabledStatus", value = buttonsEnabledStatus }));
            WriteToConsole("A gombok engedélyezve lettek", ConsoleColor.DarkMagenta);
        }

        public void ResetButtons()
        {
            try
            {
                sp.Write("r");
            }
            catch (Exception) { }
            lastPressedButton = 0;
            Send(JsonConvert.SerializeObject(new { action = "buttonReset", value = lastPressedButton }));
            WriteToConsole("A gombok resetelve lettek", ConsoleColor.DarkMagenta);
        }

        public void modeSelect(string input)
        {
            if (input == "NORMAL" || input == "HALFROBBER" || input == "FULLROBBER" || input == "EXTRA")
            {
                gameMode = input;
            }
            else
            {
                gameMode = "NORMAL";
            }

            Send(JsonConvert.SerializeObject(new { action = "modeChanged", value = gameMode }));
            WriteToConsole("Játékmód átállítva erre: " + gameMode, ConsoleColor.Gray);

            ResetTimer();
        }

        public void editTeams(dynamic json)
        {
            List<String> teamsArray = JObject.Parse(json).SelectToken("value").ToObject<List<String>>();

            if (teams.Count != 0)
            {
                teams.Clear();
            }

            int i = 0;
            foreach (string team in teamsArray)
            {
                teams.TryAdd(i, new Team { Name = team });
                i++;
            }

            Send(JsonConvert.SerializeObject(new { action = "teamsEdited", value = teamsArray.ToArray() }));
            WriteToConsole("Csapatnevek szerkesztve", ConsoleColor.Gray);
            SaveTeamList();
        }

        public void InitMode()
        {
            if (gameMode == "NORMAL" || gameMode == "HALFROBBER")
            {
                DisableButtons();
            }
            else
            {
                EnableButtons();
            }
        }

        public void StartStopTimer()
        {
            if (!secondTimer.Enabled)
            {
                if (currentTime > 0)
                {
                    ResetButtons();

                    if (currentTime == 30 && gameMode == "HALFROBBER")
                    {
                        EnableButtons();
                    }

                    secondTimer.Start();
                    Send(JsonConvert.SerializeObject(new { action = "timerStatus", value = secondTimer.Enabled }));
                    WriteToConsole("Az időzítő elindult a következő értéktől: " + currentTime, ConsoleColor.DarkYellow);
                }
            }
            else
            {
                secondTimer.Stop();
                Send(JsonConvert.SerializeObject(new { action = "timerStatus", value = secondTimer.Enabled }));
                WriteToConsole("Az időzítő leállt a következő értéknél: " + currentTime, ConsoleColor.DarkYellow);
            }
        }


        public void StartTimer()
        {
            if (currentTime >= 0 && !secondTimer.Enabled)
            {
                ResetButtons();

                if (currentTime == 30 && gameMode == "HALFROBBER")
                {
                    EnableButtons();
                }
                
                secondTimer.Start();
                Send(JsonConvert.SerializeObject(new { action = "timerStatus", value = secondTimer.Enabled }));
                WriteToConsole("Az időzítő elindult a következő értéktől: " + currentTime, ConsoleColor.DarkYellow);
            }
        }

        public void StopTimer()
        {
            if (secondTimer.Enabled)
            {
                secondTimer.Stop();
                Send(JsonConvert.SerializeObject(new { action = "timerStatus", value = secondTimer.Enabled }));
                WriteToConsole("Az időzítő leállt a következő értéknél: " + currentTime, ConsoleColor.DarkYellow);
            }
        }

        public void ResetTimer()
        {
            StopTimer();
            ResetButtons();
            InitMode();
            currentTime = 90;
            Send(JsonConvert.SerializeObject(new { action = "refreshTime", value = currentTime }));
            WriteToConsole("Az időzítő resetelve lett", ConsoleColor.DarkYellow);
        }

        public void Send(string message)
        {
            foreach (var c in clients.Keys)
            {
                c.Context.Send(message);
            }
        }

        public void CloseConnections()
        {
            SaveTeamList();
            wsServer.Stop();
            sp.Close();
        }

        public void OpenTeamList()
        {
            Serializer serializer = new Serializer();
            List<Team> deserializedObject = serializer.DeSerializeObject("Teams.activity");

            if (teams.Count != 0)
            {
                teams.Clear();
            }

            if (deserializedObject != null)
            {
                int i = 0;
                foreach (Team team in deserializedObject)
                {
                    teams.TryAdd(i, team);
                    i++;
                }
            }           
        }

        public void SaveTeamList()
        {
            Serializer serializer = new Serializer();
            List<Team> serializableObject = new List<Team>();
            foreach (var item in teams)
            {
                serializableObject.Add(item.Value);
            }
            serializer.SerializeObject("Teams.activity", serializableObject);
        }
    }
}
