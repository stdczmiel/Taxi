//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Taksówki
{
    using System;
    using System.Collections.Generic;
    
    public partial class KierowcaZlecenie
    {
        public int ID { get; set; }
        public int Kierowca { get; set; }
        public int Zlecenie { get; set; }
        public System.DateTime Poczatek { get; set; }
        public System.DateTime Koniec { get; set; }
        public Nullable<int> Czas_dojazdu { get; set; }
    
        public virtual Kierowca Kierowca1 { get; set; }
        public virtual Zlecenie Zlecenie1 { get; set; }
    }
}
