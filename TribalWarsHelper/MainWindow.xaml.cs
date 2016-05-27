using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TribalWarsHelper
{
    public partial class MainWindow : Window
    {
        public IWebDriver webDriver;
        List<AttackPlanner> attacks;
        NewAttack newAttack;
        NewAttack changeAttack;
        SaveAttackTempl saveAttackTempl;
        LoadAttackTempl loadAttackTempl;
        DispatcherTimer attackTimer;
        int currAttacksIndex;        //helpful to sending attacks
        List<Village> villList;

        public LoginData CurrLoginData { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindowClosing;
        }

        void MainWindowClosing(object sender, CancelEventArgs e)
        {
            if (webDriver != null)
                webDriver.Quit();
        }

        private void addLog(string text)
        {
            TxtLogs.AppendText(string.Format("{0}: {1}\n", DateTime.Now.ToString("hh:mm:ss"), text));
            TxtLogs.ScrollToEnd();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            EnableLoginButtons(false);
            BackgroundWorker work = new BackgroundWorker();
            work.WorkerReportsProgress = true;
            #region work.ProgressChanged
            work.ProgressChanged += (s1, e1) =>
            {
                addLog(e1.UserState as string);
            };
            #endregion
            #region work.DoWork
            work.DoWork += (s1, e1) =>         
            {

                LoginData loginData = (LoginData)e1.Argument;
                (s1 as BackgroundWorker).ReportProgress(0, string.Format("Proba logowania: {0}({1}).", loginData.Login, loginData.World));

                if (webDriver == null)
                {
                    var driverService = PhantomJSDriverService.CreateDefaultService();
                    driverService.HideCommandPromptWindow = true;
                    driverService.LoadImages = false;
                    webDriver = new PhantomJSDriver(driverService);
                }


                webDriver.Navigate().GoToUrl("https://www.plemiona.pl/");
                webDriver.FindElement(By.Id("user")).SendKeys(loginData.Login);
                webDriver.FindElement(By.Id("password")).SendKeys(loginData.Password);
                webDriver.FindElement(By.Id("cookie")).Click();
                webDriver.FindElement(By.ClassName("login_button")).SendKeys(OpenQA.Selenium.Keys.Enter);

                if (webDriver.FindElements(By.ClassName("error")).Count > 0)
                {
                    foreach (var x in webDriver.FindElements(By.ClassName("error")))
                        (s1 as BackgroundWorker).ReportProgress(0, (x.GetAttribute("innerHTML")));
                    e1.Result = false;
                    return;
                }

                (new WebDriverWait(webDriver, TimeSpan.FromSeconds(20))).
                    Until((d) => { return d.FindElement(By.ClassName("world_button_active")); });       //waiting for fully load

                bool loaded = false;

                foreach (var world in webDriver.FindElements(By.ClassName("world_button_active")))
                {
                    if (world.GetAttribute("innerHTML").Equals(String.Format("Świat {0}", loginData.World)))
                    {
                        world.Click();
                        (s1 as BackgroundWorker).ReportProgress(0, "Zalogowano.");
                        //tabControl.IsEnabled = true;        KURWA!~!!
                        loaded = true;
                        e1.Result = true;
                        CurrLoginData = loginData;
                        break;
                    }
                }
                if (!loaded)
                {
                    (s1 as BackgroundWorker).ReportProgress(0, "Błędny świat.");
                    (s1 as BackgroundWorker).ReportProgress(0, "Logowanie nieudane");
                    e1.Result = false;
                }
            };
            #endregion
            #region work.RunWorkerCompleted
            work.RunWorkerCompleted += (s1, e1) =>
            {
                if ((bool)e1.Result == true)
                {
                    tabControl.IsEnabled = true;
                }
                else
                    EnableLoginButtons(true);
            };
            #endregion

            work.RunWorkerAsync(new LoginData() { Login = TxtLogin.Text, Password = TxtPassword.Password, World = int.Parse(TxtWorld.Text.Trim()) });
        }       //multithreading

        private void EnableLoginButtons(bool enable)
        {
            if(enable==false)
            {
                BtnLogin.IsEnabled = false;
                TxtLogin.IsEnabled = false;
                TxtPassword.IsEnabled = false;
                TxtWorld.IsEnabled = false;
            }
            else
            {
                BtnLogin.IsEnabled = true;
                TxtLogin.IsEnabled = true;
                TxtPassword.IsEnabled = true;
                TxtWorld.IsEnabled = true;
            }
        } 

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker work = new BackgroundWorker();
            #region work.ProgressChanged
            work.ProgressChanged += (s1, e1) =>
            {
                addLog(e1.UserState as string);
            };
            #endregion
            work.DoWork += GetListVillages;
            #region work.RunWorkerCompleted
            work.RunWorkerCompleted += (s1, e1) =>
              {
                  villList = (List<Village>)e1.Result;
                  newAttack = new NewAttack(villList);
                  newAttack.Owner = this;
                  newAttack.Done += (s2, e2) =>
                  {
                      if (attacks == null)
                      {
                          attacks = new List<AttackPlanner>();
                          LvwAttacks.ItemsSource = attacks;
                      }
                      attacks.Add(new AttackPlanner() { Time = e2.DateTime, Src = e2.Src, Dest = e2.Dest, Army = e2.Army });
                      attacks.Sort();
                      refreshID(attacks);
                      LvwAttacks.Items.Refresh();
                  };
                  newAttack.ShowDialog();
              };
            #endregion
            work.WorkerReportsProgress = true;
            work.RunWorkerAsync();
        }       //multithreading

        private void GetListVillages(object sender, DoWorkEventArgs e)
        {
            if (villList != null)
            {
                e.Result = villList;
                return;
            }
            (sender as BackgroundWorker).ReportProgress(0, "Ładowanie listy wiosek");
            List<Village> result = new List<Village>();

            string urlInfo = webDriver.Url;
            if(GoToRefreshIfExpired(urlInfo.Remove(urlInfo.IndexOf("screen=") + 7) + "info_player"))
                (sender as BackgroundWorker).ReportProgress(0, "Sesja została odświerzona");

            IWebElement table = (new WebDriverWait(webDriver, TimeSpan.FromSeconds(5))).
                Until((d) => { return d.FindElement(By.Id("villages_list")); });        //nie pokazuje sie od razu

            var rows = table.FindElement(By.TagName("tbody")).FindElements(By.TagName("tr"));
            Village village = new Village();
            for (int i = 0; i < rows.Count; ++i)
            {
                if (i == 0)
                {
                    string url = rows[i].FindElement(By.TagName("a")).GetAttribute("href").ToString();
                    village.ID = url.Substring(url.IndexOf("id=") + 3);
                    string coordsString = rows[i].Text.Substring(5, 7);
                    village.Coords = new Point(double.Parse(coordsString.Substring(0, 3)), double.Parse(coordsString.Substring(4, 3)));
                    result.Add(village);
                    //addLog(rows[i].FindElement(By.TagName("a")).GetAttribute("href").ToString().Replace("info_village", "overview"));      //link
                }
                else if (i == 1)
                    continue;
                else if (i % 2 == 0)
                {
                    village = new Village();
                    string coordsString = rows[i].Text.Substring(5, 7);
                    village.Coords = new Point(double.Parse(coordsString.Substring(0, 3)), double.Parse(coordsString.Substring(4, 3)));
                    string url = rows[i].FindElement(By.TagName("a")).GetAttribute("href").ToString();
                    village.ID = url.Substring(url.IndexOf("id=") + 3);
                    result.Add(village);
                }
            }
            e.Result = result;
        }

        private void BtnRem_Click(object sender, RoutedEventArgs e)
        {
            if (LvwAttacks.SelectedIndex == -1)
                return;
            attacks.RemoveAt(LvwAttacks.SelectedIndex);
            refreshID(attacks);
            LvwAttacks.Items.Refresh();
        }

        private void BtnChange_Click(object sender, RoutedEventArgs e)
        {
            int index = LvwAttacks.SelectedIndex;
            if (index == -1)
                return;
            changeAttack = new NewAttack(villList) { Time = attacks[index].Time, Src = attacks[index].Src, Dest = attacks[index].Dest, Army = attacks[index].Army };
            changeAttack.Owner = this;
            changeAttack.Done += (s1, e1) =>
            {
                attacks[index].Time = changeAttack.Time;
                attacks[index].Src = changeAttack.Src;
                attacks[index].Dest = changeAttack.Dest;
                attacks[index].Army = changeAttack.Army;
                attacks.Sort();
                refreshID(attacks);
                LvwAttacks.Items.Refresh();
            };
            changeAttack.ShowDialog();
        }

        private void refreshID(List<AttackPlanner> list)
        {
            for (int i = 0; i < list.Count; ++i)
                list[i].ID = i + 1;
        }

        private void enableArmyButtons(bool enabled)
        {
            BtnAdd.IsEnabled = enabled;
            BtnChange.IsEnabled = enabled;
            BtnRem.IsEnabled = enabled;
        }

        private void BtnStartStopSetStop()
        {
            BtnStartStop.Content = "Stop";
            BtnStartStop.Click -= BtnStart_Click;
            BtnStartStop.Click += BtnStop_Click;
        }

        private void BtnStartStopSetStart()
        {
            BtnStartStop.Content = "Start";
            BtnStartStop.Click -= BtnStop_Click;
            BtnStartStop.Click += BtnStart_Click;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (attacks == null || attacks.Count == 0)
            {
                addLog("Brak ataków.");
                return;
            }

            BtnStartStopSetStop();
            enableArmyButtons(false);

            for (int i = 0; i < attacks.Count; ++i)
            {
                if (attacks[i].Time < DateTime.Now.AddSeconds(10))
                {
                    addLog(String.Format("{0}. Czas ataku minął.", attacks[i].ID));
                    continue;
                }
                currAttacksIndex = i;
                attackTimer = new DispatcherTimer();
                attackTimer.Interval = attacks[currAttacksIndex].Time - DateTime.Now;
                attackTimer.Tick += SendAttack;
                attackTimer.Start();
                addLog(string.Format("{0}. Oczekiwanie na atak, pozostało: {1}.", attacks[currAttacksIndex].ID,
                    (attacks[currAttacksIndex].Time - DateTime.Now).ToString(@"dd\.hh\:mm\:ss")));
                return;
            }
            addLog("Zakończono wysyłanie ataków.");

            BtnStartStopSetStart();
            enableArmyButtons(true);

        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            addLog("Wstrzymano wysłanie ataków.");
            if (attackTimer != null)
                attackTimer.Stop();
            enableArmyButtons(true);
            BtnStartStopSetStart();
        }

        private void SendAttack(object sender, EventArgs e)
        {
            BackgroundWorker work = new BackgroundWorker();
            work.WorkerReportsProgress = true;
            #region work.ProgressChanged
            work.ProgressChanged += (s1, e1) =>
            {
                addLog(e1.UserState as string);
            };
            #endregion
            #region work.DoWork
            work.DoWork += (s1, e1) =>
            {
                attackTimer.Stop();
                (s1 as BackgroundWorker).ReportProgress(0,String.Format("{0}. Wysyłanie ataku.", attacks[currAttacksIndex].ID));

                if (GoToRefreshIfExpired(String.Format("https://pl{0}.plemiona.pl/game.php?village={1}&screen=place", CurrLoginData.World, attacks[currAttacksIndex].Src.ID)))
                    (s1 as BackgroundWorker).ReportProgress(0, "Sesja została odświerzona");

                webDriver.FindElement(By.Name("input")).SendKeys(string.Format("{0}|{1}",
                    attacks[currAttacksIndex].Dest.Coords.X, attacks[currAttacksIndex].Dest.Coords.Y));

                #region settingUnitValue
                webDriver.FindElement(By.Id("unit_input_spear")).SendKeys(attacks[currAttacksIndex].Army.SpearFighter.ToString());
                webDriver.FindElement(By.Id("unit_input_sword")).SendKeys(attacks[currAttacksIndex].Army.Swordman.ToString());
                webDriver.FindElement(By.Id("unit_input_axe")).SendKeys(attacks[currAttacksIndex].Army.Archer.ToString());
                webDriver.FindElement(By.Id("unit_input_archer")).SendKeys(attacks[currAttacksIndex].Army.Archer.ToString());
                webDriver.FindElement(By.Id("unit_input_spy")).SendKeys(attacks[currAttacksIndex].Army.Scout.ToString());
                webDriver.FindElement(By.Id("unit_input_light")).SendKeys(attacks[currAttacksIndex].Army.LightCalvary.ToString());
                webDriver.FindElement(By.Id("unit_input_marcher")).SendKeys(attacks[currAttacksIndex].Army.MountedArcher.ToString());
                webDriver.FindElement(By.Id("unit_input_heavy")).SendKeys(attacks[currAttacksIndex].Army.HeavyCalvary.ToString());
                webDriver.FindElement(By.Id("unit_input_ram")).SendKeys(attacks[currAttacksIndex].Army.Ram.ToString());
                webDriver.FindElement(By.Id("unit_input_catapult")).SendKeys(attacks[currAttacksIndex].Army.Catapult.ToString());
                webDriver.FindElement(By.Id("unit_input_knight")).SendKeys(attacks[currAttacksIndex].Army.Paladin.ToString());
                webDriver.FindElement(By.Id("unit_input_snob")).SendKeys(attacks[currAttacksIndex].Army.Nobleman.ToString());
                #endregion

                webDriver.FindElement(By.Id("target_attack")).Click();

                if (webDriver.FindElements(By.ClassName("error_box")).Count > 0)
                {
                    foreach (var error in webDriver.FindElements(By.ClassName("error_box")))
                    {
                        (s1 as BackgroundWorker).ReportProgress(0, string.Format("{0}. {1}", currAttacksIndex + 1, error.GetAttribute("innerHTML").Trim()));
                    }
                }
                else
                {
                    IJavaScriptExecutor ex = (IJavaScriptExecutor)webDriver;
                    ex.ExecuteScript("document.getElementById(\"troop_confirm_go\").click()");
                    (s1 as BackgroundWorker).ReportProgress(0, string.Format("{0}. Atak został wysłany", currAttacksIndex + 1));
                }

                //Kolejny atak
                if (currAttacksIndex + 1 == attacks.Count)
                {
                    (s1 as BackgroundWorker).ReportProgress(0, "Zakończono wysyłanie ataków.");
                    e1.Result = true;       //ended
                    return;
                }
                ++currAttacksIndex;

                #pragma warning disable 0162        //disable warning about unreachable code in loop
                for (; currAttacksIndex < attacks.Count; ++currAttacksIndex)
                {
                    if (attacks[currAttacksIndex].Time < DateTime.Now)
                    {
                        (s1 as BackgroundWorker).ReportProgress(0, string.Format("{0}. Atak wykonany z opóźnieniem.", attacks[currAttacksIndex].ID));
                        e1.Result = false;
                        return;
                    }
                    else
                    {
                        attackTimer.Interval = attacks[currAttacksIndex].Time - DateTime.Now;
                        break;
                    }
                }

                (s1 as BackgroundWorker).ReportProgress(0, string.Format("{0}. Oczekiwanie na atak, pozostało: {1}.", attacks[currAttacksIndex].ID,
                    (attacks[currAttacksIndex].Time - DateTime.Now).ToString(@"dd\.hh\:mm\:ss")));
                attackTimer.Start(); //wykonuje sie na podstawowym watku
            };
            #endregion
            #region work.RunWorkerCompleted
            work.RunWorkerCompleted += (s1, e1) =>
            {
                bool? isEnded = (bool?)e1.Result;
                if (isEnded==true)
                {
                    enableArmyButtons(true);
                    BtnStartStopSetStart();
                }
                else if(isEnded==false)
                {
                    SendAttack(this, EventArgs.Empty);
                }

            };
            #endregion
            work.RunWorkerAsync();
        }

        private bool GoToRefreshIfExpired(string url)
        {
            webDriver.Navigate().GoToUrl(url);
            if (!webDriver.PageSource.Contains("Sesja wygasła"))
            {
                return false;
            }
            #region Logging
            webDriver.Navigate().GoToUrl("https://www.plemiona.pl/");
            webDriver.FindElement(By.Id("user")).SendKeys(CurrLoginData.Login);
            webDriver.FindElement(By.Id("password")).SendKeys(CurrLoginData.Password);
            webDriver.FindElement(By.Id("cookie")).Click();
            webDriver.FindElement(By.ClassName("login_button")).SendKeys(OpenQA.Selenium.Keys.Enter);

            if (webDriver.FindElements(By.ClassName("error")).Count > 0)
            {
                foreach (var x in webDriver.FindElements(By.ClassName("error")))
                    throw(new Exception(x.GetAttribute("innerHTML")));      //trzeba cos z tym zrobic
            }

            (new WebDriverWait(webDriver, TimeSpan.FromSeconds(10))).
            Until((d) => { return d.FindElement(By.ClassName("world_button_active")); });       //waiting for fully load

            foreach (var world in webDriver.FindElements(By.ClassName("world_button_active")))
            {
                if (world.GetAttribute("innerHTML").Equals(String.Format("Świat {0}", CurrLoginData.World)))
                {
                    world.Click();
                    break;
                }
            }
            #endregion
            webDriver.Navigate().GoToUrl(url);
            return true;
        }

        private void BtnSaveTempl_Click(object sender, RoutedEventArgs e)
        {
            if (attacks == null || attacks.Count == 0)
            {
                MessageBox.Show("Brak ataków do zapisania", "TribalWarsHelper");
                return;
            }
            saveAttackTempl = new SaveAttackTempl();
            saveAttackTempl.Owner = this;
            saveAttackTempl.Done += SaveAttackTemplFunction;
            saveAttackTempl.ShowDialog();
        }

        private void SaveAttackTemplFunction(object sender, SaveAttackTemplEventArgs e)
        {
            Directory.CreateDirectory("AttackTemplates");
            string filePath = String.Format("./AttackTemplates/{0}", e.FileName);
            if(File.Exists(filePath))
            {
                var result = MessageBox.Show("Szablon już istnieje, czy chcesz go nadpisać?", "TribalWarsHelper", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    addLog("Zapisywanie szablonu zatrzymane");
                    return;
                }
            }
            using (StreamWriter stream = new StreamWriter(filePath))
                foreach (var x in attacks)
                {
                    //{0} to róźnica między pierwszym atakiem, a każdym kolejnym
                    stream.Write(String.Format("{0},{1},{2},{3}\n", x.Time - attacks[0].Time, x.Src, x.Dest, x.Army));
                    stream.Flush();
                }

        }

        private void BtnLoadTempl_Click(object sender, RoutedEventArgs e)
        {
            if(attacks!=null && attacks.Count>0)
            {
                var result=MessageBox.Show("Wczytanie szablonu usunie aktualnie zaplanowane ataki, czy chcesz kontynuuować?", "TribalWarsHelper", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;
            }
            if(!Directory.Exists((@".\AttackTemplates")))
            {
                MessageBox.Show("Nie znaleziono szablonów!", "TribalWarsHelper");
                return;
            }
            string[] templates = Directory.GetFiles(@".\AttackTemplates");
            if (templates.Length == 0)
            {
                MessageBox.Show("Nie znaleziono szablonów!", "TribalWarsHelper");
                return;
            }
            loadAttackTempl = new LoadAttackTempl(templates);
            loadAttackTempl.Owner = this;
            loadAttackTempl.Done += LoadAttackTemplFunction;
            loadAttackTempl.ShowDialog();

        }

        private void LoadAttackTemplFunction(object sender, LoadAttackTemplEventArgs e)
        {
            if (attacks == null)
            {
                attacks = new List<AttackPlanner>();
                LvwAttacks.ItemsSource = attacks;
            }
            attacks.Clear();
            StreamReader reader = new StreamReader(e.FilePath);
            string line = reader.ReadLine();
            while (line!=null)
            {
                string[] splittedLine = line.Split(',');
                AttackPlanner attack = new AttackPlanner();
                attack.Time = e.StartTime + TimeSpan.Parse(splittedLine[0]);
                attack.Src = Village.Parse(splittedLine[1]);
                attack.Dest = Village.Parse(splittedLine[2]);
                attack.Army = ArmyClass.Parse(splittedLine[3]);
                attacks.Add(attack);
                line = reader.ReadLine();
            }
            refreshID(attacks);
            LvwAttacks.Items.Refresh();            
            
        }
    }
    public partial class AttackPlanner: IComparable
    {
        public int ID { get; set; }
        public DateTime Time { get; set; }
        public Village Src { get; set; }
        public Village Dest { get; set; }
        public ArmyClass Army { get; set; }
        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            AttackPlanner other = obj as AttackPlanner;
            return DateTime.Compare(this.Time, other.Time);
        }
    }
    public partial class ArmyClass
    {
        public int SpearFighter { get; set; }
        public int Swordman { get; set; }
        public int Axeman { get; set; }
        public int Archer { get; set; }
        public int Scout { get; set; }
        public int LightCalvary { get; set; }
        public int MountedArcher { get; set; }
        public int HeavyCalvary { get; set; }
        public int Ram { get; set; }
        public int Catapult { get; set; }
        public int Paladin { get; set; }
        public int Nobleman { get; set; }
        public ArmyClass() { }

        public int this[int number]
        {
            get
            {
                switch(number)
                {
                    case 0: return SpearFighter;
                    case 1: return Swordman;
                    case 2: return Axeman;
                    case 3: return Archer;
                    case 4: return Scout;
                    case 5: return LightCalvary;
                    case 6: return MountedArcher;
                    case 7: return HeavyCalvary;
                    case 8: return Ram;
                    case 9: return Catapult;
                    case 10: return Paladin;
                    case 11: return Nobleman;
                    default: throw new IndexOutOfRangeException();                   
                }
            }
            set
            {
                switch(number)
                {
                    case 0: { SpearFighter = value; break; }
                    case 1: { Swordman = value; break; }
                    case 2: { Axeman = value; break; }
                    case 3: { Archer = value; break; }
                    case 4: { Scout = value; break; }
                    case 5: { LightCalvary = value; break; }
                    case 6: { MountedArcher = value; break; }
                    case 7: { HeavyCalvary = value; break; }
                    case 8: { Ram = value; break; }
                    case 9: { Catapult = value; break; }
                    case 10: { Paladin = value; break; }
                    case 11: { Nobleman = value; break; }
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
        public override string ToString()
        {
            return String.Format("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}",
                SpearFighter, Swordman, Axeman, Archer, Scout, LightCalvary, MountedArcher, HeavyCalvary, Ram, Catapult, Paladin, Nobleman);
        }

        public static ArmyClass Parse(string armyClassString)        //format like in ToString()
        {
            ArmyClass result = new ArmyClass();
            string[] army = armyClassString.Split('|');
            for (int i = 0; i < army.Length; ++i)
            {
                result[i] = int.Parse(army[i]);
            }
            return result;
        }
    }
    public partial class Village
    {
        public Point Coords { get; set; }
        public string ID { get; set; }
        public Village() { }
        public override string ToString()
        {
            return String.Format("{0}|{1}", Coords.X, Coords.Y);
        }

        static public Village Parse(string villageString)
        {
            Village vill = new Village();
            vill.Coords = new Point(double.Parse(villageString.Substring(0, 3)), double.Parse(villageString.Substring(4, 3)));
            return vill;
        }
    }
    public partial class LoginData
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public int World { get; set; }
    }
}
