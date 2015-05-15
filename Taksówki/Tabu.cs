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

        Queue<Kierowca_Zlecenie> kolejkaTabu;
        const int dlugoscKolejkiTabu = 5;
        public double funkcjaCelu;

        public Tabu(taxiEntities te)
        {
            dbTaxiContext = te;

            funkcjaCelu = double.MaxValue;
            kolejkaTabu = new Queue<Kierowca_Zlecenie>();
        }

        public void UlozHarmonogram()
        {
            WyczyscTabeleKierowcaZlecenie();
            PobierzHarmonogram();
            WstepnePrzyporzadkowanie();
            Algorytm();
        }

        void WyczyscTabeleKierowcaZlecenie()
        {
            dbTaxiContext.Database.ExecuteSqlCommand("TRUNCATE TABLE Kierowca-Zlecenie");
        }

        void PobierzHarmonogram()
        {
            var k =   from driver in dbTaxiContext.Kierowca
                      select driver;
            kierowcy = new ObservableCollection<Kierowca>(k);
            if (kierowcy.Count() == 0)
                throw new IndexOutOfRangeException("Nie ma zadnych kierowcow");

            var z = from commissions in dbTaxiContext.Zlecenie
                      where commissions.Czas_poczatkowy >= DateTime.Now
                      orderby commissions.Czas_poczatkowy ascending
                    select commissions;
            zlecenia = new ObservableCollection<Zlecenie>(z);
            if(zlecenia.Count()==0)
                throw new IndexOutOfRangeException("Nie ma zadnych zlecen");
        }


        void WstepnePrzyporzadkowanie()
        {
            int iloscKierowcow = kierowcy.Count();               
           
            // Dodajemy zlecenia po kolei do wszystkich kierowcow
            int idxKierowca = 0;
            foreach (Zlecenie z in zlecenia)
            {
                Kierowca_Zlecenie kz = new Kierowca_Zlecenie();
                kz.Kierowca1 = kierowcy[idxKierowca];
                kz.Zlecenie1 = z;
                kierowcy[idxKierowca].Kierowca_Zlecenie.Add(kz);
                PrzeliczCzasy(kierowcy[idxKierowca]); 

                // ustaw kolejnego kierowcę
                if (++idxKierowca >= iloscKierowcow)
                {
                    idxKierowca = 0;
                }
            }
                       
        }

        void Algorytm()
        {
            Kierowca kierNajdluzsze = kierowcy.First();
            int indeksNajdluzsze = 0;
            double najdluzszyCzasDojazdu = 0;

            // szukamy zadania o najdluzszym czasie dojazdu
            foreach (Kierowca k in kierowcy)
            {
                for (int i = 0; i < k.Kierowca_Zlecenie.Count(); i++)
                {
                    if (k.Kierowca_Zlecenie.ElementAt(i).Czas_dojazdu > najdluzszyCzasDojazdu)
                    {
                        if (!kolejkaTabu.Contains(k.Kierowca_Zlecenie.ElementAt(i)))    // sprawdzenie, czy zadanie nie jest na liscie tabu
                        {
                            najdluzszyCzasDojazdu = k.Kierowca_Zlecenie.ElementAt(i).Czas_dojazdu;
                            kierNajdluzsze = k;
                            indeksNajdluzsze = i;
                        }
                    }
                }
            }

            // dodajemy zadanie na kolejke tabu
            kolejkaTabu.Enqueue(kierNajdluzsze.Kierowca_Zlecenie.ElementAt(indeksNajdluzsze));
            if (kolejkaTabu.Count() > dlugoscKolejkiTabu)
            {
                kolejkaTabu.Dequeue();
            }

            // probujemy wstawic je na kazde mozliwe miejsce
            foreach (Kierowca k in kierowcy)
            {
                // przechodzimy do godziny, od której mozna wstawic nasze zadanie
                int idx = 0;
                while ((idx < k.Kierowca_Zlecenie.Count()) && (k.Kierowca_Zlecenie.ElementAt(idx).Poczatek < kierNajdluzsze.Kierowca_Zlecenie.ElementAt(indeksNajdluzsze).Zlecenie1.Czas_poczatkowy))
                {
                    idx++;
                }

                do
                {
                    if (PrzestawJesliLepsze(kierNajdluzsze, indeksNajdluzsze, k, idx))
                    {
                        kierNajdluzsze = k;
                        indeksNajdluzsze = idx;
                    }
                    idx++;
                } while (idx < k.Kierowca_Zlecenie.Count());

            }
        }

        void PrzeliczCzasy(Kierowca k)
        {
            // czasy dla pierwszego zlecenia
            k.Kierowca_Zlecenie.First().Czas_dojazdu = 0;
            k.Kierowca_Zlecenie.First().Poczatek = k.Kierowca_Zlecenie.First().Zlecenie1.Czas_poczatkowy;
            k.Kierowca_Zlecenie.First().Koniec = k.Kierowca_Zlecenie.First().Poczatek;
            k.Kierowca_Zlecenie.First().Koniec.AddMinutes(k.Kierowca_Zlecenie.First().Zlecenie1.Przyblizony_czas_drogi);

            // czasy dla pozostalych zlecen
            for (int i = 1; i < k.Kierowca_Zlecenie.Count();i++ )
            {
                Kierowca_Zlecenie poprz = k.Kierowca_Zlecenie.ElementAt(i-1);
                Kierowca_Zlecenie akt = k.Kierowca_Zlecenie.ElementAt(i);
                akt.Czas_dojazdu = CzasDojazdu(poprz.Zlecenie1.Dokad_szer, poprz.Zlecenie1.Dokad_dl, akt.Zlecenie1.Skad_szer, akt.Zlecenie1.Skad_dl);

                // czas rozpoczęcia zlecenia jest uzależniony od tego, kiedy zakończyło się poprzednie i jak długi jest czas dojazdu do klienta
                if (poprz.Koniec + TimeSpan.FromMinutes(akt.Czas_dojazdu) < akt.Zlecenie1.Czas_poczatkowy)
                {
                    akt.Poczatek = akt.Zlecenie1.Czas_poczatkowy;
                }
                else
                {
                    akt.Poczatek = poprz.Koniec + TimeSpan.FromMinutes(akt.Czas_dojazdu);
                }
            }
        }

        // Sprawdza czy ograniczenia sa spelnione w aktualnym harmonogramie
        // 1. Opoznienie nie jest wieksze niz dopuszczalne
        // (2. Roznica w czasie pracy miedzy kierowcami nie przekracza 1h)
        bool SprawdzOgraniczenia()
        {
            foreach (Kierowca k in kierowcy)
            {
                foreach (Kierowca_Zlecenie kz in k.Kierowca_Zlecenie)
                {
                    if(kz.Poczatek > kz.Zlecenie1.Czas_poczatkowy + TimeSpan.FromMinutes(kz.Zlecenie1.Mozliwe_spoznienie))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Zwraca wartosc funkcji celu (łączny czas dojazdów) dla obecnego harmonogramu.
        // Czasy dojazdów są proporcjonalne do zużycia paliwa.
        double FunkcjaCelu()
        {
            double fc = 0;
            foreach (Kierowca k in kierowcy)
            {
                foreach (Kierowca_Zlecenie kz in k.Kierowca_Zlecenie)
                {
                    fc += kz.Czas_dojazdu;
                }
            }
            return fc;
        }

        static int CompareZlecenia(Kierowca_Zlecenie a, Kierowca_Zlecenie b)
        {
            return a.Poczatek.CompareTo(b.Poczatek);    // zwraca ujemną liczbę, jeśli a jest wcześniej
        }

        bool PrzestawJesliLepsze(Kierowca k1, int indeks1, Kierowca k2, int indeks2)
        {
            List<Kierowca_Zlecenie> k1KZ = k1.Kierowca_Zlecenie.ToList();
            //k1KZ.Sort(CompareZlecenia);
            List<Kierowca_Zlecenie> k2KZ = k2.Kierowca_Zlecenie.ToList();
            //k2KZ.Sort(CompareZlecenia);

            if (k1 == k2 && indeks1 == indeks2)
            {
                return false;
            }

            k2KZ.Insert(indeks2, k1KZ[indeks1]);
            k1KZ.RemoveAt(indeks1);
            k2KZ[indeks2].Kierowca1 = k2;
            PrzeliczCzasy(k1);
            PrzeliczCzasy(k2);

            double nowaFunkcjaCelu = FunkcjaCelu();

            // jesli nowy harmonogram nie spelnia ograniczen lub jest gorszy od poprzedniego, 
            // to wroc do poprzedniego harmonogramu
            if (!SprawdzOgraniczenia() || (nowaFunkcjaCelu > funkcjaCelu))
            {                
                k1KZ.Insert(indeks1, k2KZ[indeks2]);
                k2KZ.RemoveAt(indeks2);
                PrzeliczCzasy(k1);
                PrzeliczCzasy(k2);
                k1KZ[indeks1].Kierowca1 = k1;
                return false;
            }
            else
            {
                funkcjaCelu = nowaFunkcjaCelu;
                return true;
            }
        }

        // odleglosc w kilometrach (przy zalozeniu 1 stopien = 111 km )
        decimal Odleglosc(decimal szer1, decimal dlug1, decimal szer2, decimal dlug2)
        {
            decimal stala = 111.1M;
            return (Math.Abs(dlug2 - dlug1) + Math.Abs(szer2 - szer1)) * stala;
        }

        // czas przejazdu w minutach (przy zalozeniu predkosci 50km/h)
        int CzasDojazdu(decimal szer1, decimal dlug1, decimal szer2, decimal dlug2)
        {
            return (int)(Odleglosc(szer1, dlug1, szer2, dlug2) * 60.0M / 50.0M); 
        }
    }
}
