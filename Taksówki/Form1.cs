using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using System.Diagnostics;
using GMap.NET.WindowsForms.Markers;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace Taksówki
{
    public partial class Form1 : Form
    {
        Tabu TS;
        string baseTime;
        DateTime startTime;
        private List<ChartEvent> events;
        
        private taxiEntities dbTaxiContext = new taxiEntities();
        
        int currentZlecenieId = 0;

        bool isMouseDown = false;
        bool markerMoved = false;

        #region gMap
        readonly GMapOverlay top = new GMapOverlay();
        internal readonly GMapOverlay objects = new GMapOverlay("objects");
        internal readonly GMapOverlay routes = new GMapOverlay("routes");

        GMapMarker currentMarker;
        public Form1()
        {
            InitializeComponent();
            TS = new Tabu(dbTaxiContext);
            try
            {
                System.Net.IPHostEntry e =
                     System.Net.Dns.GetHostEntry("www.google.com");
            }
            catch
            {
                gMap1.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection avaible, going to CacheOnly mode.",
                      "GMap.NET - Demo.WindowsForms", MessageBoxButtons.OK,
                      MessageBoxIcon.Warning);
            }

            // config map
            gMap1.MapProvider = GMapProviders.OpenStreetMap;
            gMap1.Position = new PointLatLng(51.110, 17.030);
            gMap1.MinZoom = 0;
            gMap1.MaxZoom = 24;
            gMap1.Zoom = 12;
            // map events
            {
                gMap1.MouseDown += new MouseEventHandler(MainMap_MouseDown);
                gMap1.MouseMove += new MouseEventHandler(MainMap_MouseMove);
                gMap1.MouseDoubleClick += new MouseEventHandler(MainMap_MouseDoubleClick);
                gMap1.OnMarkerClick += new MarkerClick(MainMap_OnMarkerClick);
                gMap1.MouseUp += new MouseEventHandler(MainMap_MouseUp);
            }
            gMap1.Overlays.Add(routes);
            gMap1.Overlays.Add(objects);
            gMap1.Overlays.Add(top);
            // set current marker
        }

        void MainMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (tabControl1.SelectedIndex == 1 && top.Markers.Count < 2)
            {
                currentMarker = new GMarkerGoogle(gMap1.FromLocalToLatLng(e.X, e.Y), GMarkerGoogleType.red);
                if (top.Markers.Count == 0)
                {
                    currentMarker.ToolTipText = "Początek trasy";
                    skad_szerTextBox.Text = Math.Round(currentMarker.Position.Lat, 6).ToString();
                    skad_dlTextBox.Text =  Math.Round(currentMarker.Position.Lng,6).ToString();
                }
                else
                {
                    currentMarker.ToolTipText = "Koniec trasy";
                    dokad_szerTextBox.Text = Math.Round(currentMarker.Position.Lat,6).ToString();
                    dokad_dlTextBox.Text = Math.Round(currentMarker.Position.Lng,6).ToString();
                }
                currentMarker.ToolTipMode = MarkerTooltipMode.Always;
                
                top.Markers.Add(currentMarker);
                if (top.Markers.Count == 2)
                    findRoute();
            }
        }

        void MainMap_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                isMouseDown = true;
        }

        void MainMap_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            currentMarker = item;
        }

        void MainMap_MouseUp(object sender, MouseEventArgs e)
        {
            if (tabControl1.SelectedIndex == 1 && e.Button == MouseButtons.Left && top.Markers.Count == 2 && markerMoved)
            {
                if (currentMarker.ToolTipText == "Poczatek trasy")
                {
                    skad_szerTextBox.Text = currentMarker.Position.Lat.ToString();
                    skad_dlTextBox.Text = currentMarker.Position.Lng.ToString();
                }
                else
                {
                    dokad_szerTextBox.Text = currentMarker.Position.Lat.ToString();
                    dokad_dlTextBox.Text = currentMarker.Position.Lng.ToString();
                }
                findRoute();
            }
            markerMoved = false;
            isMouseDown = false;
        }

        void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (tabControl1.SelectedIndex == 1 && e.Button == MouseButtons.Left && isMouseDown)
            {
                if (currentMarker != null && currentMarker.IsVisible)
                {
                    currentMarker.Position = gMap1.FromLocalToLatLng(e.X, e.Y);
                    markerMoved = true;
                }
            }
        }

        private void findRoute()
        {
            RoutingProvider rp = gMap1.MapProvider as RoutingProvider;
            if (rp == null)
            {
                rp = GMapProviders.GoogleMap; // use OpenStreetMap if provider does not implement routing
            }
            PointLatLng start = top.Markers.ElementAt(0).Position;
            PointLatLng end = top.Markers.ElementAt(1).Position;
            MapRoute route = rp.GetRoute(start, end, false, false, (int)gMap1.Zoom);
            if (route != null)
            {
                // add route
                routes.Clear();
                GMapRoute r = new GMapRoute(route.Points, route.Name);
                r.IsHitTestVisible = true;
                routes.Routes.Add(r);
                Debug.Print(r.Name);
                if (rp.Equals(GMapProviders.GoogleMap))
                {
                    String[] splitted = r.Name.Split('/');
                    if (splitted.Count() > 0)
                        przyblizony_czas_drogiTextBox.Text = splitted.ElementAt(1).TrimEnd(" mins)".ToCharArray()).Trim();
                }
                else
                    przyblizony_czas_drogiTextBox.Text = Math.Round((r.Distance / 50.0)*60, 0).ToString();
                gMap1.ZoomAndCenterRoute(r);
                distanceTB.Text = Math.Round(r.Distance,2).ToString();
            }
            
        }

        public void clearMap()
        {
            top.Markers.Clear();
            routes.Clear();
        }

        public void addDriversToMap()
        {
            var driversAtWork = from drivers in dbTaxiContext.Kierowca
                                from driversStatus in dbTaxiContext.Status_kierowcy
                                where drivers.ID_kierowcy == driversStatus.Kierowca
                                where driversStatus.W_pracy == true
                                select new {drivers.ID_kierowcy, drivers.Imie, drivers.Nazwisko, driversStatus};
            top.Markers.Clear();
            routes.Routes.Clear();

            foreach (var driver in driversAtWork)
            {
                PointLatLng pos = new PointLatLng(Convert.ToDouble(driver.driversStatus.Pozycja_szer), Convert.ToDouble(driver.driversStatus.Pozycja_dl));
                currentMarker = new GMarkerGoogle(pos, GMarkerGoogleType.red);
                currentMarker.ToolTipText = driver.Imie + " " + driver.Nazwisko;
                currentMarker.ToolTipMode = MarkerTooltipMode.Always;
                top.Markers.Add(currentMarker);
            }
        }
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the '_baza_danychDataSet.Status_kierowcy' table. You can move, or remove it, as needed.
            this.status_kierowcyTableAdapter.Fill(this._baza_danychDataSet.Status_kierowcy);
            // TODO: This line of code loads data into the '_baza_danychDataSet.Zlecenie' table. You can move, or remove it, as needed.
            this.zlecenieTableAdapter.Fill(this._baza_danychDataSet.Zlecenie);
            // TODO: This line of code loads data into the '_baza_danychDataSet.Samochod' table. You can move, or remove it, as needed.
            this.samochodTableAdapter.Fill(this._baza_danychDataSet.Samochod);
            // TODO: This line of code loads data into the '_baza_danychDataSet.Kierowca' table. You can move, or remove it, as needed.
            this.kierowcaTableAdapter.Fill(this._baza_danychDataSet.Kierowca);

            zlecenieDataGridView_CellClick(zlecenieDataGridView, new DataGridViewCellEventArgs(0, 0));
            addDriversToMap();
            Gantt();
        }

        #region formEvents
        private void kierowcaBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.kierowcaBindingSource.EndEdit();
            this.tableAdapterManager.UpdateAll(this._baza_danychDataSet);
        }

        private void samochodBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.samochodBindingSource.EndEdit();
            this.tableAdapterManager.UpdateAll(this._baza_danychDataSet);
        }

        private void status_KierowcyBindingNavigatorSaveItem_Click(object sender, EventArgs e)
        {
            this.Validate();
            this.status_kierowcyBindingSource.EndEdit();
            this.tableAdapterManager.UpdateAll(this._baza_danychDataSet);
            addDriversToMap();
        }

        private void zlecenieDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Zaktualizowanie formy
            if (e.RowIndex >=0 && e.RowIndex < this._baza_danychDataSet.Zlecenie.Count())
            {
                currentZlecenieId = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).ID_zlecenie;
                skad_dlTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Skad_dl.ToString();
                skad_szerTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Skad_szer.ToString();
                dokad_dlTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Dokad_dl.ToString();
                dokad_szerTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Dokad_szer.ToString();
                czas_poczatkowyDateTimePicker.Value = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Czas_poczatkowy;
                przyblizony_czas_drogiTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Przyblizony_czas_drogi.ToString();
                mozliwe_spoznienieTextBox.Text = this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Mozliwe_spoznienie.ToString();
                checkBoxVip.Checked = int.Parse(this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).VIP.ToString()) == 1 ? true : false;

                // Zaktualizowanie mapy
                top.Markers.Clear();
                routes.Routes.Clear();
                PointLatLng start = new PointLatLng(Convert.ToDouble(this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Skad_szer), Convert.ToDouble(this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Skad_dl));
                PointLatLng stop = new PointLatLng(Convert.ToDouble(this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Dokad_szer), Convert.ToDouble(this._baza_danychDataSet.Zlecenie.ElementAt(e.RowIndex).Dokad_dl));
                currentMarker = new GMarkerGoogle(start, GMarkerGoogleType.red);
                currentMarker.ToolTipText = "Początek trasy";
                currentMarker.ToolTipMode = MarkerTooltipMode.Always;
                top.Markers.Add(currentMarker);
                currentMarker = new GMarkerGoogle(stop, GMarkerGoogleType.red);
                currentMarker.ToolTipText = "Koniec trasy";
                currentMarker.ToolTipMode = MarkerTooltipMode.Always;
                top.Markers.Add(currentMarker);
                findRoute();
            }
        
        }

        private void applyZlecenieFormButton_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(dokad_dlTextBox.Text) || string.IsNullOrWhiteSpace(dokad_szerTextBox.Text)||
                string.IsNullOrWhiteSpace(skad_dlTextBox.Text) || string.IsNullOrWhiteSpace(skad_szerTextBox.Text) ||
                string.IsNullOrWhiteSpace(przyblizony_czas_drogiTextBox.Text) ||
                string.IsNullOrWhiteSpace(mozliwe_spoznienieTextBox.Text) || czas_poczatkowyDateTimePicker.Value < DateTime.Now)
            {
                MessageBox.Show("Nie zostały wypełnione wszystkie pola!","Błąd", MessageBoxButtons.OK,MessageBoxIcon.Exclamation,MessageBoxDefaultButton.Button1);
            }
            else
            {
                Zlecenie comission;
                if (currentZlecenieId < 0)
                {        
                    comission = new Zlecenie();
                    comission.Skad_dl = decimal.Parse(skad_dlTextBox.Text);
                    comission.Skad_szer = decimal.Parse(skad_szerTextBox.Text);
                    comission.Dokad_dl = decimal.Parse(dokad_dlTextBox.Text);
                    comission.Dokad_szer = decimal.Parse(dokad_szerTextBox.Text);
                    comission.Mozliwe_spoznienie = int.Parse(mozliwe_spoznienieTextBox.Text);
                    comission.Przyblizony_czas_drogi = int.Parse(przyblizony_czas_drogiTextBox.Text);
                    comission.VIP = checkBoxVip.Checked ? true : false;
                    comission.Czas_poczatkowy = ConvertToDateTime(czas_poczatkowyDateTimePicker.Text);
                    
                    dbTaxiContext.Zlecenie.Add(comission);
                    dbTaxiContext.SaveChanges();
                }
                else
                {
                    var zlecenieQuery = from zl in dbTaxiContext.Zlecenie where zl.ID_zlecenie == currentZlecenieId select zl;
                    comission = zlecenieQuery.Single();
                    comission.Skad_dl = decimal.Parse(skad_dlTextBox.Text);
                    comission.Skad_szer = decimal.Parse(skad_szerTextBox.Text);
                    comission.Dokad_dl = decimal.Parse(dokad_dlTextBox.Text);
                    comission.Dokad_szer = decimal.Parse(dokad_szerTextBox.Text);
                    comission.Mozliwe_spoznienie = int.Parse(mozliwe_spoznienieTextBox.Text);
                    comission.Przyblizony_czas_drogi = int.Parse(przyblizony_czas_drogiTextBox.Text);
                    comission.VIP = checkBoxVip.Checked ? true : false;
                    comission.Czas_poczatkowy = ConvertToDateTime(czas_poczatkowyDateTimePicker.Text);
                    dbTaxiContext.SaveChanges();

                }
                this.zlecenieTableAdapter.Fill(this._baza_danychDataSet.Zlecenie);
                zlecenieDataGridView.Update();
                TS.UlozHarmonogram();
            }
        }

        private void buttonNewComission_Click(object sender, EventArgs e)
        {
            clearZlecenieForm();
            clearMap();
            Gantt();
        }

        public void clearZlecenieForm()
        {
            currentZlecenieId = -1;
            skad_dlTextBox.Text = "";
            skad_szerTextBox.Text = "";
            dokad_dlTextBox.Text = "";
            dokad_szerTextBox.Text = "";
            czas_poczatkowyDateTimePicker.Value = DateTime.Now.AddMinutes(30);
            przyblizony_czas_drogiTextBox.Text = "0";
            mozliwe_spoznienieTextBox.Text = "0";
            checkBoxVip.Checked = false;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearMap();
            switch (tabControl1.SelectedIndex)
            {
                case 0:                 // kierowcy
                    addDriversToMap();
                    break;
                case 1:                 // zlecenia
                    zlecenieDataGridView_CellClick(zlecenieDataGridView, new DataGridViewCellEventArgs(0, 0));
                    break;
                default:
                    break;
            }
        }
        #endregion

        private DateTime ConvertToDateTime(string strDateTime)
        {
            DateTime dtFinaldate;
            string sDateTime;
            try { dtFinaldate = Convert.ToDateTime(strDateTime); }
            catch (Exception e)
            {
                string[] sDate = strDateTime.Split(' ');
                string[] date = sDate[0].Split(':');
                string[] time = sDate[1].Split(':');
                sDateTime = date[2] + '-' + date[1] + '-' + date[0] + " " + time[0] + ":" + time[1] + ":" + time[2];
                dtFinaldate = Convert.ToDateTime(sDateTime);
            }
            return dtFinaldate;
        }

        #region query

        //private void getSchedule()
        //{

        //    var schedule = from commissions in dbTaxiContext.Zlecenie
        //                   from commissions_drivers in dbTaxiContext.Kierowca_Zlecenie
        //                   from driver in dbTaxiContext.Kierowca
        //                   where commissions.ID_zlecenie == commissions_drivers.Zlecenie
        //                   where driver.ID_kierowcy == commissions_drivers.Kierowca
        //                   where commissions_drivers.Poczatek == null
        //                   where commissions.Czas_poczatkowy >= DateTime.Now
        //                   select new { commissions.ID_zlecenie, };

        //}
        //#endregion

        #region gantt
        public void Gantt(string baseTime = "hours", List<ChartEvent> e = null)
        {
            this.baseTime = baseTime;
            events = new List<ChartEvent>();
            startTime = DateTime.Now;
            var commisions = from com in dbTaxiContext.Zlecenie
                             where com.Czas_poczatkowy > DateTime.Now
                             select com;
            foreach(var com in commisions)
            {
                
                ChartEvent chartEvent = new ChartEvent(com.Czas_poczatkowy, com.Przyblizony_czas_drogi, com.Mozliwe_spoznienie, com.ID_zlecenie.ToString());
                events.Add(chartEvent);
            }
            itemBar.Controls.Clear();
            timeBar.Controls.Clear();
            chartPanel.Controls.Clear();
            drawItemsOnChart();
        }

        private void drawTimeBar()
        {
            ChartEvent maxDateEvent = events.ElementAt(findCmax());
            DateTime maxDate = maxDateEvent.EventStartTime.AddMinutes(maxDateEvent.PossibleDelay + maxDateEvent.EventLength);
            int hours = (int)Math.Ceiling(maxDate.AddHours(1).Subtract(startTime).TotalHours);
            int days = (int)Math.Ceiling(maxDate.AddHours(1).Subtract(startTime).TotalDays);
            for (int i = 0; i < days; ++i)
            {
                Label label0 = new Label();
                label0.Text = startTime.AddDays(i).Day.ToString() + "-" + startTime.AddDays(i).Month.ToString() + "-" + startTime.AddDays(i).Year.ToString();
                label0.TextAlign = ContentAlignment.MiddleCenter;
                label0.BorderStyle = BorderStyle.FixedSingle;
                Point position0;
                if (i == 0)
                {
                    label0.Width = (24 - startTime.Hour) * 60;
                    position0 = new Point(0, 1);
                }
                else
                {
                    label0.Width = 24 * 60;
                    position0 = new Point(((24 - startTime.Hour) * 60) + (24 * 60 * (i-1)), 1);
                }
                label0.Height = 25;
                label0.Padding = new Padding(0);
                label0.Margin = new Padding(0);
                
                label0.Location = position0;
                timeBar.Controls.Add(label0);
            }

            for (int i = 0; i < hours; ++i)
            {
                Label label1 = new Label();
                label1.Text = startTime.AddHours(i).Hour.ToString() + ":00";
                label1.TextAlign = ContentAlignment.MiddleLeft;
                label1.BorderStyle = BorderStyle.FixedSingle;
                label1.Width = 60;
                label1.Height = 25;
                label1.Padding = new Padding(0);
                label1.Margin = new Padding(0);
                Point position1 = new Point(60*i, 25);
                label1.Location = position1;
                timeBar.Controls.Add(label1);
            }
        }

        private void drawItemBar()
        {
            itemBar.FlowDirection = FlowDirection.TopDown;

            foreach (ChartEvent ev in events)
            {
                Label label1 = new Label();
                label1.Text = ev.EventCaption;
                label1.TextAlign = ContentAlignment.MiddleLeft;
                label1.BorderStyle = BorderStyle.FixedSingle;
                label1.Width = 80;
                label1.Height = 50;
                label1.Padding = new Padding(0);
                label1.Margin = new Padding(0);
                itemBar.Controls.Add(label1);
            }
        }

        private void drawItemsOnChart()
        {
            drawTimeBar();
            drawItemBar();
            chartPanel.Width = timeBar.Width;
            chartPanel.Height = itemBar.Height;
            int currPos = 0;

            int i = 0;
            foreach (ChartEvent ev in events)
            {
                Label orangeLabel = new Label();
                orangeLabel.BackColor = Color.Orange;
                orangeLabel.Height = 50;
                orangeLabel.Width = ev.PossibleDelay;
                orangeLabel.Padding = new Padding(0);
                orangeLabel.Margin = new Padding(0);
                Point position1 = new Point((int)(ev.EventStartTime.Subtract(startTime).TotalMinutes) + startTime.Minute, 50 * i);
                orangeLabel.Location = position1;

                Label redLabel = new Label();
                redLabel.BackColor = Color.Red;
                redLabel.Height = 50;
                redLabel.Width = ev.EventLength;
                redLabel.Padding = new Padding(0);
                redLabel.Margin = new Padding(0);
                Point position2 = new Point((int)(ev.EventStartTime.Subtract(startTime).TotalMinutes) + startTime.Minute + ev.PossibleDelay, 50 * i);
                redLabel.Location = position2;
                ToolTip toolTip = new ToolTip();
                
                chartPanel.Controls.Add(redLabel);
                toolTip.SetToolTip(redLabel, ev.EventCaption);
                chartPanel.Controls.Add(orangeLabel);
                i++;
            }
            while (currPos < timeBar.Width)
            {
                Label label1 = new Label();
                label1.BorderStyle = BorderStyle.FixedSingle;
                label1.Width = 60;
                label1.Height = itemBar.Height;
                label1.Padding = new Padding(0);
                label1.Margin = new Padding(0);
                label1.SendToBack();
                label1.Name = "vertical" + currPos;
                Point position1 = new Point(currPos, 0);
                label1.Location = position1;
                chartPanel.Controls.Add(label1);
                chartPanel.Controls.Find("vertical" + currPos, false).First().SendToBack();
                currPos += 60;

            }
        }

        int findCmax()
        {
            int maxIndex = 0, i = 0;
            DateTime maxDate = DateTime.Now;
            DateTime currDate;
            foreach (ChartEvent ev in events)
            {
                currDate = ev.EventStartTime.AddMinutes(ev.PossibleDelay + ev.EventLength);
                if ( currDate > maxDate)
                {
                    maxIndex = i;
                    maxDate = currDate;
                }
                i++;
            }
            return maxIndex;
        }

        private void chartPanel_Click(object sender, EventArgs e)
        {
            labelhover.Text = (MousePosition.X - chartPanel.Location.X).ToString() +" " + (MousePosition.Y - chartPanel.Location.Y).ToString();
        }

        #endregion

    }
    public partial class ChartEvent
    {
        public DateTime EventStartTime { get; private set; }
        public int PossibleDelay { get; private set; }
        public int EventLength { get; private set; }
        public string EventCaption { get; private set; }

        public ChartEvent(DateTime start, int length, int delay = 0, string caption = "")
        {
            EventStartTime = start;
            EventLength = length;
            PossibleDelay = delay;
            EventCaption = caption;
        }
    }
           
}
