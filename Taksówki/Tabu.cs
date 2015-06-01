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

// W bazie nie powinno być tabeli KierowcaZlecenie
// Zamiast tego Poczatek i Koniec powinny być w tabeli Zlecenie

namespace Taksówki
{
    class Tabu
    {

        const int dlugoscKolejkiTabu = 5;
        const int iloscIteracji = 10;


        private taxiEntities dbTaxiContext;
        public ObservableCollection<Kierowca> kierowcy;
        public ObservableCollection<Zlecenie> zlecenia;
        List<List<KierowcaZlecenie>> l;

        Queue<KierowcaZlecenie> kolejkaTabu;
        public double funkcjaCelu;
    
        public Tabu(taxiEntities te)
        {
            dbTaxiContext = te;

            funkcjaCelu = double.MaxValue;
            kolejkaTabu = new Queue<KierowcaZlecenie>();
        }


        // Podstawowa funkcja wywołująca wszystkie po kolei
        public void UlozHarmonogram()
        {
            WyczyscTabeleKierowcaZlecenie();
            PobierzHarmonogram();
            WstepnePrzyporzadkowanie();
            for (int i = 0; i < iloscIteracji; i++)
            {
                Algorytm();
            }
            ZaktualizujBazeDanych();
        }

        void ZaktualizujBazeDanych()
        {
            dbTaxiContext.SaveChanges();
        }

        // Tabela jest za kazdym razem układana od nowa, więc starą można usunąc
        void WyczyscTabeleKierowcaZlecenie()
        {
            dbTaxiContext.KierowcaZlecenie.RemoveRange(dbTaxiContext.KierowcaZlecenie);
            dbTaxiContext.SaveChanges();
        }

        // Pobiera tabele Kierowca i Zlecenie z bazy danych
        void PobierzHarmonogram()
        {
            var k = from driver in dbTaxiContext.Kierowca
                    orderby driver.ID_kierowcy ascending
                    select driver;
            kierowcy = new ObservableCollection<Kierowca>(k);
            if (kierowcy.Count() == 0)
                throw new IndexOutOfRangeException("Nie ma zadnych kierowcow");

            DateTime startTime = DateTime.Now + TimeSpan.FromDays(1);
            DateTime endTime = DateTime.Now + +TimeSpan.FromDays(1) + TimeSpan.FromHours(24);
            var z = from commissions in dbTaxiContext.Zlecenie
                    where commissions.Czas_poczatkowy >= startTime
                    where commissions.Czas_poczatkowy <= endTime
                    orderby commissions.Czas_poczatkowy ascending
                    select commissions;
            zlecenia = new ObservableCollection<Zlecenie>(z);
            if (zlecenia.Count() == 0)
                throw new IndexOutOfRangeException("Nie ma zadnych zlecen");
        }

        // Wstawia zlecenia po kolei do kierowcow, czyli pierwsze do pierwszego, drugie do drugiego, ... I od początku potem.
        void WstepnePrzyporzadkowanie()
        {
            int iloscKierowcow = dbTaxiContext.Kierowca.Count();

            int idxKierowca = 0;
            foreach (Zlecenie z in zlecenia)
            {
                KierowcaZlecenie kz = new KierowcaZlecenie();
                kz.Poczatek = DateTime.Now;
                kz.Koniec = DateTime.Now;
                //kz.ID = IDkz++;
                dbTaxiContext.KierowcaZlecenie.Add(kz);

                kz.Kierowca1 = kierowcy[idxKierowca];
                kz.Zlecenie1 = z;

                // ustaw kolejnego kierowcę, albo wróć do pierwszego
                if (++idxKierowca >= iloscKierowcow)
                {
                    idxKierowca = 0;
                }
            }


            
            dbTaxiContext.SaveChanges();

            // Aktualizacja robi się chyba automatycznie
            //var k = from driver in dbTaxiContext.Kierowca
            //        orderby driver.ID_kierowcy ascending
            //        select driver;
            //kierowcy = new ObservableCollection<Kierowca>(k);
            //if (kierowcy.Count() == 0)
            //    throw new IndexOutOfRangeException("Nie ma zadnych kierowcow");

            // Tworzone są Listy dla kierowcow - używa się ich wygodniej niż tabeli KierowcaZlecenie.
            // Każda lista odzwierciedla zlecenia dla kierowcy. Czyli mozna uzywac tak : l[indeksKierowcy][numerZlecenia]
            l = new List<List<KierowcaZlecenie>>(kierowcy.Count());
            for (int i = 0; i < kierowcy.Count(); i++)
            {
                l.Add(new List<KierowcaZlecenie>());
                l[i] = kierowcy[i].KierowcaZlecenie.ToList();
            }

            foreach (List<KierowcaZlecenie> lista in l)
            {
                // lista.Sort(CompareZlecenia2); // niepotrzebne, bo zlecenie i tak są posortowane (orderby ascending)
                PrzeliczCzasy(lista);
            }


        }


        // Algorytm Tabu Search
        void Algorytm()
        {

            int kierNajdluzsze = 0;
            int indeksNajdluzsze = 0;
            int najdluzszyCzasDojazdu = 0;

            int kierpom = 0, indexpom = 0;

            // Szukamy zadania o najdluzszym czasie dojazdu i bedziemy je wstawiac na kazde mozliwe miejsce.
            // W algorytmie TS sprawdzalismy wszystkie zadania ze sciezki krytycznej, ale tu chyba nie ma sciezki krytycznej.
            for (int k = 0; k < l.Count(); k++)
            {
                for (int i = 0; i < l[k].Count(); i++)
                {
                    if (l[k][i].Czas_dojazdu >= najdluzszyCzasDojazdu)
                    {
                        kierpom = k; indexpom = i;
                        if (!kolejkaTabu.Contains(l[k][i]))    // sprawdzenie, czy zadanie nie jest na liscie tabu
                        {
                            najdluzszyCzasDojazdu = (int)l[k][i].Czas_dojazdu;
                            kierNajdluzsze = k;
                            indeksNajdluzsze = i;
                        }
                    }
                }
            }

            // jesli zlecenie nie istnieje, to wybierz najstarsze z kolejki
            if (l[kierNajdluzsze].Count() <= indeksNajdluzsze)
            {
                kierNajdluzsze = kierpom;
                indeksNajdluzsze = indexpom;
            }

            // dodajemy zadanie na kolejke tabu
            kolejkaTabu.Enqueue(l[kierNajdluzsze][indeksNajdluzsze]);
            if (kolejkaTabu.Count() > dlugoscKolejkiTabu)
            {
                kolejkaTabu.Dequeue();
            }

            // probujemy wstawic je na kazde mozliwe miejsce
            for (int k = 0; k < l.Count(); k++)
            {
                // Przechodzimy do godziny, od której mozna wstawic nasze zadanie.
                // Czyli szukamy indeksu zadania, które zaczyna się nie wczesniej niz zadany czas początkowy
                int idx = 0;
                while ((idx < l[k].Count()-1) && (l[k][idx].Poczatek < l[kierNajdluzsze][indeksNajdluzsze].Zlecenie1.Czas_poczatkowy))
                {
                    idx++;
                }

                // Wstawiamy zadanie na każde miejsce od tej godziny aż do końca
                do
                {
                    if (PrzestawJesliLepsze(kierNajdluzsze, indeksNajdluzsze, k, idx))
                    {
                        kierNajdluzsze = k;
                        indeksNajdluzsze = idx;
                    }
                    idx++;
                } while (idx < l[k].Count());

            }

        }

        // Aktualizuje Początek, Koniec i Czas_dojazdu dla każdego zadania.
        // W maszynach nazywało się to C, P i r.
        void PrzeliczCzasy(List<KierowcaZlecenie> l)
        {
            // czasy dla pierwszego zlecenia
            if (l.Count() > 0)
            {
                l.First().Czas_dojazdu = 0;
                l.First().Poczatek = l.First().Zlecenie1.Czas_poczatkowy;
                l.First().Koniec = l.First().Poczatek;
                l.First().Koniec = l.First().Koniec.AddMinutes((double)l.First().Zlecenie1.Przyblizony_czas_drogi);
            }

            // czasy dla pozostalych zlecen
            for (int i = 1; i < l.Count(); i++)
            {
                KierowcaZlecenie poprz = l.ElementAt(i - 1);
                KierowcaZlecenie akt = l.ElementAt(i);
                akt.Czas_dojazdu = CzasDojazdu(poprz.Zlecenie1.Dokad_szer, poprz.Zlecenie1.Dokad_dl, akt.Zlecenie1.Skad_szer, akt.Zlecenie1.Skad_dl);

                // czas rozpoczęcia zlecenia jest uzależniony od tego, kiedy zakończyło się poprzednie i jak długi jest czas dojazdu do klienta
                if (poprz.Koniec + TimeSpan.FromMinutes((double)akt.Czas_dojazdu) < akt.Zlecenie1.Czas_poczatkowy)
                {
                    akt.Poczatek = akt.Zlecenie1.Czas_poczatkowy.Subtract(TimeSpan.FromMinutes((double)akt.Czas_dojazdu));
                }
                else
                {
                    akt.Poczatek = poprz.Koniec;
                }
                akt.Koniec = akt.Poczatek.AddMinutes((double)akt.Zlecenie1.Przyblizony_czas_drogi);
            }
        }

        // Sprawdza czy ograniczenia sa spelnione w aktualnym harmonogramie
        // 1. Opoznienie nie jest wieksze niz dopuszczalne
        // (2. Roznica w czasie pracy miedzy kierowcami nie przekracza 1h)
        bool SprawdzOgraniczenia()
        {
            for (int k = 0; k < l.Count(); k++)
            {
                for (int i = 0; i < l[k].Count(); i++)
                {
                    if (l[k][i].Poczatek > l[k][i].Zlecenie1.Czas_poczatkowy + TimeSpan.FromMinutes(l[k][i].Zlecenie1.Mozliwe_spoznienie))
                    {
                        return false;
                    }

                }
            }
            return true;
        }


        // Zwraca wartosc funkcji celu (uwzglednia tylko czas dojazdów) dla obecnego harmonogramu.
        // Czasy dojazdów są proporcjonalne do zużycia paliwa.
        double FunkcjaCelu()
        {
            double fc = 0;
            for (int k = 0; k < l.Count(); k++)
            {
                for (int i = 0; i < l[k].Count(); i++)
                {
                    fc += (double)l[k][i].Czas_dojazdu;
                    double opoznienie = (l[k][i].Poczatek - l[k][i].Zlecenie1.Czas_poczatkowy).TotalMinutes;
                    fc += opoznienie;
                }
            }
            return fc;
        }

        // Funkcja potrzebna do posortowania według pola Poczatek
        static int CompareZlecenia(KierowcaZlecenie a, KierowcaZlecenie b)
        {
            return a.Poczatek.CompareTo(b.Poczatek);    // zwraca ujemną liczbę, jeśli a jest wcześniej
        }

        // Funkcja potrzebna do posortowania według pola Czas_poczatkowy
        static int CompareZlecenia2(KierowcaZlecenie a, KierowcaZlecenie b)
        {
            return a.Zlecenie1.Czas_poczatkowy.CompareTo(b.Zlecenie1.Czas_poczatkowy);
        }

        // Przestawia zadanie z l[k1][indeks1] do l[k2][indeks2] jesli nowa funkcja celu jest lepsza i spelnia ograniczenia
        bool PrzestawJesliLepsze(int k1, int indeks1, int k2, int indeks2)
        {
            if (k1 == k2 && indeks1 == indeks2)
            {
                return false;
            }

            l[k2].Insert(indeks2, l[k1][indeks1]);
            //kierowcy[k2].KierowcaZlecenie.Add(l[k1][indeks1]);   // niepotrzebne, jeśli w aktualizacji przepiszamy listy l do kierowców.KierowcaZlecenie
            //kierowcy[k1].KierowcaZlecenie.Remove(l[k1][indeks1]);
            l[k1].RemoveAt(indeks1);
            

            l[k2][indeks2].Kierowca1 = kierowcy[k2];
            l[k2][indeks2].Kierowca = kierowcy[k2].ID_kierowcy;
            PrzeliczCzasy(l[k1]);
            PrzeliczCzasy(l[k2]);

            double nowaFunkcjaCelu = FunkcjaCelu();

            // jesli nowy harmonogram nie spelnia ograniczen lub jest gorszy od poprzedniego, 
            // to wroc do poprzedniego harmonogramu
            //if (!SprawdzOgraniczenia() || (nowaFunkcjaCelu > funkcjaCelu))
            if (nowaFunkcjaCelu > funkcjaCelu)
            {
                l[k1].Insert(indeks1, l[k2][indeks2]);
                //kierowcy[k1].KierowcaZlecenie.Add(l[k2][indeks2]);
                //kierowcy[k2].KierowcaZlecenie.Remove(l[k2][indeks2]);
                l[k2].RemoveAt(indeks2);

                PrzeliczCzasy(l[k1]);
                PrzeliczCzasy(l[k2]);
                l[k1][indeks1].Kierowca1 = kierowcy[k1];
                l[k1][indeks1].Kierowca = kierowcy[k1].ID_kierowcy;
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
