using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Collections.ObjectModel;

namespace Taksówki
{
    class Tabu
    {
        private taxiEntities dbTaxiContext;
        public ObservableCollection<Kierowca> kierowcy;
        public ObservableCollection<Zlecenie> zlecenia;

        public Tabu(taxiEntities te)
        {
            dbTaxiContext = te;
        }

        public void UlozHarmonogram()
        {
            bool rezultat;
            WyczyscTabeleKierZlec();
            PobierzHarmonogram();
            WstepnePrzyporzadkowanie();
            Algorytm();
        }

        private void WyczyscTabeleKierZlec()
        {

        }

        private void PobierzHarmonogram()
        {
            var k = from commissions in dbTaxiContext.Zlecenie
                      from commissions_drivers in dbTaxiContext.Kierowca_Zlecenie
                      from driver in dbTaxiContext.Kierowca
                      where commissions.ID_zlecenie == commissions_drivers.Zlecenie
                      where driver.ID_kierowcy == commissions_drivers.Kierowca
                      where commissions.Czas_poczatkowy >= DateTime.Now
                      select driver;
            kierowcy = new ObservableCollection<Kierowca>(k);

            var z = from commissions in dbTaxiContext.Zlecenie
                      from commissions_drivers in dbTaxiContext.Kierowca_Zlecenie
                      from driver in dbTaxiContext.Kierowca
                      where commissions.ID_zlecenie == commissions_drivers.Zlecenie
                      where driver.ID_kierowcy == commissions_drivers.Kierowca
                      where commissions.Czas_poczatkowy >= DateTime.Now
                      orderby commissions.Czas_poczatkowy ascending
                    select commissions;
            zlecenia = new ObservableCollection<Zlecenie>(z);
        }

        private void WstepnePrzyporzadkowanie()
        {
            int iloscKierowcow = kierowcy.Count();
            if (iloscKierowcow == 0)
                
            int idxKierowca = 0;
            // Dodajemy zlecenia od najwczesniejszego
            foreach (Zlecenie z in zlecenia)
            {

            }
        }

        private void Algorytm()
        {

        }

     
    }
}
