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
    
    public partial class uzytkownicy
    {
        public uzytkownicy()
        {
            this.zdarzenia = new HashSet<zdarzenia>();
        }
    
        public int iduzytkownicy { get; set; }
        public string login { get; set; }
        public string haslo { get; set; }
        public string uprawnienia { get; set; }
    
        public virtual ICollection<zdarzenia> zdarzenia { get; set; }
    }
}
