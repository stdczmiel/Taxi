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
using Taksówki.CustomMarkers;
using System.Diagnostics;
using GMap.NET.WindowsForms.Markers;

namespace Taksówki
{
    public partial class Form1 : Form
    {
        readonly GMapOverlay top = new GMapOverlay();
        internal readonly GMapOverlay objects = new GMapOverlay("objects");
        internal readonly GMapOverlay routes = new GMapOverlay("routes");

        // marker
        GMapMarker currentMarker;

        bool isMouseDown = false;
        bool markerMoved = false;

        public Form1()
        {
            InitializeComponent();

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

            // add your custom map db provider
            //GMap.NET.CacheProviders.MySQLPureImageCache ch = new GMap.NET.CacheProviders.MySQLPureImageCache();
            //ch.ConnectionString = @"server=sql2008;User Id=trolis;Persist Security Info=True;database=gmapnetcache;password=trolis;";
            //gMap1.Manager.SecondaryCache = ch;

            // set your proxy here if need
            //GMapProvider.WebProxy = new WebProxy("10.2.0.100", 8080);
            //GMapProvider.WebProxy.Credentials = new NetworkCredential("ogrenci@bilgeadam.com", "bilgeada");

            // map events
            {
                gMap1.MouseDown += new MouseEventHandler(MainMap_MouseDown);
                gMap1.MouseMove += new MouseEventHandler(MainMap_MouseMove);
                gMap1.MouseDoubleClick += new MouseEventHandler(MainMap_MouseDoubleClick);
                gMap1.OnMarkerClick += new MarkerClick(MainMap_OnMarkerClick);
                gMap1.MouseUp += new MouseEventHandler(MainMap_MouseUp);
     /*         gMap1.OnPositionChanged += new PositionChanged(gMap1_OnPositionChanged);

                gMap1.OnTileLoadStart += new TileLoadStart(gMap1_OnTileLoadStart);
                gMap1.OnTileLoadComplete += new TileLoadComplete(gMap1_OnTileLoadComplete);

                gMap1.OnMapZoomChanged += new MapZoomChanged(gMap1_OnMapZoomChanged);
                gMap1.OnMapTypeChanged += new MapTypeChanged(gMap1_OnMapTypeChanged);

                gMap1.OnMarkerClick += new MarkerClick(gMap1_OnMarkerClick);
                gMap1.OnMarkerEnter += new MarkerEnter(gMap1_OnMarkerEnter);
                gMap1.OnMarkerLeave += new MarkerLeave(gMap1_OnMarkerLeave);

                gMap1.OnPolygonEnter += new PolygonEnter(gMap1_OnPolygonEnter);
                gMap1.OnPolygonLeave += new PolygonLeave(gMap1_OnPolygonLeave);

                gMap1.OnRouteEnter += new RouteEnter(gMap1_OnRouteEnter);
                gMap1.OnRouteLeave += new RouteLeave(gMap1_OnRouteLeave);

                gMap1.Manager.OnTileCacheComplete += new TileCacheComplete(OnTileCacheComplete);
                gMap1.Manager.OnTileCacheStart += new TileCacheStart(OnTileCacheStart);
                gMap1.Manager.OnTileCacheProgress += new TileCacheProgress(OnTileCacheProgress);*/
            }
            gMap1.Overlays.Add(routes);
            gMap1.Overlays.Add(objects);
            gMap1.Overlays.Add(top);
            // set current marker
        }

        void MainMap_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (top.Markers.Count < 2)
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
            if (e.Button == MouseButtons.Left && top.Markers.Count == 2 && markerMoved)
            {
                
                findRoute();
            }
            markerMoved = false;
            isMouseDown = false;
        }

        void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isMouseDown)
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

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the '_baza_danychDataSet.Zlecenie' table. You can move, or remove it, as needed.
            this.zlecenieTableAdapter.Fill(this._baza_danychDataSet.Zlecenie);
            // TODO: This line of code loads data into the '_baza_danychDataSet.Samochod' table. You can move, or remove it, as needed.
            this.samochodTableAdapter.Fill(this._baza_danychDataSet.Samochod);
            // TODO: This line of code loads data into the '_baza_danychDataSet.Kierowca' table. You can move, or remove it, as needed.
            this.kierowcaTableAdapter.Fill(this._baza_danychDataSet.Kierowca);

        }

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

    }
           
}
