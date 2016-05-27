using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace TribalWarsHelper
{
    public partial class NewAttack : Window
    {
        private List<Village> villList;

        public DateTime Time
        {
            get { return dateTimePicker.Value==null?DateTime.Now:(DateTime)dateTimePicker.Value; }
            set { dateTimePicker.Value = value; }
        }
        public Village Src
        {
            get
            {
                return villList[CbxVillSrc.SelectedIndex];
            }
            set
            {
                CbxVillSrc.SelectedIndex = villList.FindIndex(
                    (vill) => { return vill.Coords == value.Coords; }
                    );
            }
        }
        public Village Dest
        {
            get
            {
                return TxtVillDest.IsMaskCompleted ?
                    new Village() { Coords = new Point(int.Parse(TxtVillDest.Text.Substring(0, 3)), int.Parse((TxtVillDest.Text.Substring(0, 3)))) } :
                    new Village() { Coords = new Point(0, 0) };
            }
            set { TxtVillDest.Text = String.Format("{0}|{1}", value.Coords.X, value.Coords.Y); }
        }
        public ArmyClass Army
        {
            get
            {
                try
                {
	                return new ArmyClass()
	                {
	                    SpearFighter = int.Parse(TxtSpearFighter.Text.Trim().Length == 0 ? "0" : TxtSpearFighter.Text),
	                    Swordman = int.Parse(TxtSwordman.Text.Trim().Length == 0 ? "0" : TxtSwordman.Text),
	                    Axeman = int.Parse(TxtAxeman.Text.Trim().Length == 0 ? "0" : TxtAxeman.Text),
	                    Archer = int.Parse(TxtArcher.Text.Trim().Length == 0 ? "0" : TxtArcher.Text),
	                    Scout = int.Parse(TxtScout.Text.Trim().Length == 0 ? "0" : TxtScout.Text),
	                    LightCalvary = int.Parse(TxtLightCalvary.Text.Trim().Length == 0 ? "0" : TxtLightCalvary.Text),
	                    MountedArcher = int.Parse(TxtMountedArcher.Text.Trim().Length == 0 ? "0" : TxtMountedArcher.Text),
	                    HeavyCalvary = int.Parse(TxtHeavyCalvary.Text.Trim().Length == 0 ? "0" : TxtHeavyCalvary.Text),
	                    Ram = int.Parse(TxtRam.Text.Trim().Length == 0 ? "0" : TxtRam.Text),
	                    Catapult = int.Parse(TxtCatapult.Text.Trim().Length == 0 ? "0" : TxtCatapult.Text),
	                    Paladin = int.Parse(TxtPaladin.Text.Trim().Length == 0 ? "0" : TxtPaladin.Text),
	                    Nobleman = int.Parse(TxtNobleman.Text.Trim().Length == 0 ? "0" : TxtNobleman.Text)
	                };
                }
                catch (Exception)
                {
                    MessageBox.Show("Wystapił błąd podczas wybierania wojska!");
                    return new ArmyClass();

                }
            }
            set
            {
                TxtSpearFighter.Text = value.SpearFighter.ToString();
                TxtSwordman.Text = value.Swordman.ToString();
                TxtAxeman.Text = value.Axeman.ToString();
                TxtArcher.Text = value.Archer.ToString();
                TxtScout.Text = value.Scout.ToString();
                TxtLightCalvary.Text = value.LightCalvary.ToString();
                TxtMountedArcher.Text = value.MountedArcher.ToString();
                TxtHeavyCalvary.Text = value.HeavyCalvary.ToString();
                TxtRam.Text = value.Ram.ToString();
                TxtCatapult.Text = value.Catapult.ToString();
                TxtPaladin.Text = value.Paladin.ToString();
                TxtNobleman.Text = value.Nobleman.ToString();

            }
        }

        public NewAttack(List<Village> villList)  
        {
            InitializeComponent();
            //TextBox_Name.AddHandler(FrameworkElement.MouseLeftButtonDownEvent, new MouseButtonEventHandler("Name_Textbox_MouseLeftButtonDown"), true);
            this.villList = villList;
            CbxVillSrc.ItemsSource = from x in villList
                                   select x.ToString();
            dateTimePicker.Value = DateTime.Now;
        }

        public event EventHandler<NewAttackEventArgs> Done;

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if(Done!=null)
                Done(this, new NewAttackEventArgs(Time, Src, Dest, Army));
        }

        private void AlignInputToLeft(object sender, System.Windows.Input.MouseEventArgs e)
        {
            MessageBox.Show("DUPA");
        }
    }
    public partial class NewAttackEventArgs : EventArgs
    {
        public NewAttackEventArgs(DateTime dateTime, Village src, Village dest, ArmyClass army)
        {
            DateTime = dateTime;
            Src = src;
            Dest = dest;
            Army = army;
        }
        public DateTime DateTime { get; set; }
        public Village Src { get; set; }
        public Village Dest { get; set; }
        public ArmyClass Army { get; set; }

    }

}
