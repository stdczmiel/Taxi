﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class taxiEntities : DbContext
    {
        public taxiEntities()
            : base("name=taxiEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Kierowca> Kierowca { get; set; }
        public virtual DbSet<Samochod> Samochod { get; set; }
        public virtual DbSet<Status_kierowcy> Status_kierowcy { get; set; }
        public virtual DbSet<Zlecenie> Zlecenie { get; set; }
        public virtual DbSet<Kierowca_Zlecenie> Kierowca_Zlecenie { get; set; }
        public virtual DbSet<listwy> listwy { get; set; }
        public virtual DbSet<magazyn> magazyn { get; set; }
        public virtual DbSet<ustawienia> ustawienia { get; set; }
        public virtual DbSet<uzytkownicy> uzytkownicy { get; set; }
        public virtual DbSet<zdarzenia> zdarzenia { get; set; }
        public virtual DbSet<listwa_magazyn> listwa_magazyn { get; set; }
    }
}
