using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Concreta.Models.Entities;

namespace Concreta.Data
{
    /// <summary>
    /// Contexto database-first: la base la crea database/ConcretaDB.sql y EF nunca la modifica.
    /// </summary>
    public class ConcretaDbContext : DbContext
    {
        static ConcretaDbContext()
        {
            Database.SetInitializer<ConcretaDbContext>(null);
        }

        public ConcretaDbContext() : base("name=ConcretaDB")
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<TipoMaterial> TiposMaterial { get; set; }
        public DbSet<Material> Materiales { get; set; }
        public DbSet<ParametroAguaAire> ParametrosAguaAire { get; set; }
        public DbSet<ParametroRelacionAC> ParametrosRelacionAC { get; set; }
        public DbSet<ParametroAgregadoGrueso> ParametrosAgregadoGrueso { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<DisenoMezcla> Disenos { get; set; }
        public DbSet<DetalleMezcla> DetallesMezcla { get; set; }
        public DbSet<EnsayoLaboratorio> Ensayos { get; set; }
        public DbSet<Comparacion> Comparaciones { get; set; }
        public DbSet<ComparacionDetalle> ComparacionesDetalle { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            mb.Conventions.Remove<PluralizingTableNameConvention>();
            mb.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            // ---------- Seguridad ----------
            mb.Entity<Rol>().HasKey(x => x.IdRol);
            mb.Entity<Usuario>().HasKey(x => x.IdUsuario);
            mb.Entity<Usuario>().HasRequired(x => x.Rol).WithMany(r => r.Usuarios).HasForeignKey(x => x.IdRol);

            // ---------- Materiales ----------
            mb.Entity<TipoMaterial>().HasKey(x => x.IdTipoMaterial);
            var mat = mb.Entity<Material>();
            mat.HasKey(x => x.IdMaterial);
            mat.HasRequired(x => x.TipoMaterial).WithMany(t => t.Materiales).HasForeignKey(x => x.IdTipoMaterial);
            mat.Property(x => x.PesoEspecifico).HasPrecision(5, 3);
            mat.Property(x => x.PorcentajeAdicion).HasPrecision(5, 2);
            mat.Property(x => x.ModuloFineza).HasPrecision(4, 2);
            mat.Property(x => x.PesoUnitarioCompactado).HasPrecision(7, 2);
            mat.Property(x => x.Absorcion).HasPrecision(5, 2);
            mat.Property(x => x.ContenidoHumedad).HasPrecision(5, 2);
            mat.Property(x => x.CostoKg).HasPrecision(10, 4);
            mat.Property(x => x.FactorCO2Kg).HasPrecision(10, 5);

            // ---------- Parámetros ACI ----------
            var aa = mb.Entity<ParametroAguaAire>();
            aa.HasKey(x => x.IdParametro);
            aa.Property(x => x.AsentamientoMin).HasPrecision(4, 1);
            aa.Property(x => x.AsentamientoMax).HasPrecision(4, 1);
            aa.Property(x => x.AguaLitrosM3).HasPrecision(6, 1);
            aa.Property(x => x.AireAtrapado).HasPrecision(4, 2);

            var rac = mb.Entity<ParametroRelacionAC>();
            rac.HasKey(x => x.IdParametro);
            rac.Property(x => x.RelacionAC).HasPrecision(4, 3);

            var ag = mb.Entity<ParametroAgregadoGrueso>();
            ag.HasKey(x => x.IdParametro);
            ag.Property(x => x.ModuloFineza).HasPrecision(4, 2);
            ag.Property(x => x.VolumenM3).HasPrecision(4, 3);

            // ---------- Diseño ----------
            var pro = mb.Entity<Proyecto>();
            pro.HasKey(x => x.IdProyecto);
            pro.HasRequired(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);

            var dm = mb.Entity<DisenoMezcla>();
            dm.HasKey(x => x.IdDiseno);
            dm.HasRequired(x => x.Proyecto).WithMany(p => p.Disenos).HasForeignKey(x => x.IdProyecto);
            dm.HasRequired(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            dm.Property(x => x.AsentamientoPulg).HasPrecision(4, 1);
            dm.Property(x => x.RelacionAC).HasPrecision(4, 3);
            dm.Property(x => x.AireAtrapado).HasPrecision(4, 2);
            dm.Property(x => x.CostoTotalM3).HasPrecision(10, 2);
            dm.Property(x => x.HuellaCO2M3).HasPrecision(10, 2);

            var det = mb.Entity<DetalleMezcla>();
            det.HasKey(x => x.IdDetalle);
            det.HasRequired(x => x.Diseno).WithMany(d => d.Detalles).HasForeignKey(x => x.IdDiseno).WillCascadeOnDelete(true);
            det.HasRequired(x => x.Material).WithMany().HasForeignKey(x => x.IdMaterial);
            det.Property(x => x.CantidadKgM3).HasPrecision(10, 2);
            det.Property(x => x.VolumenAbsolutoM3).HasPrecision(8, 4);
            det.Property(x => x.CostoParcial).HasPrecision(10, 2);
            det.Property(x => x.CO2Parcial).HasPrecision(10, 3);

            var ens = mb.Entity<EnsayoLaboratorio>();
            ens.HasKey(x => x.IdEnsayo);
            ens.HasRequired(x => x.Diseno).WithMany(d => d.Ensayos).HasForeignKey(x => x.IdDiseno);
            ens.HasRequired(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            ens.Property(x => x.ValorObtenido).HasPrecision(10, 2);
            ens.Property(x => x.FechaEnsayo).HasColumnType("date");

            // ---------- Comparación ----------
            var cmp = mb.Entity<Comparacion>();
            cmp.HasKey(x => x.IdComparacion);
            cmp.HasRequired(x => x.Usuario).WithMany().HasForeignKey(x => x.IdUsuario);
            cmp.HasOptional(x => x.DisenoRecomendado).WithMany().HasForeignKey(x => x.IdDisenoRecomendado);

            var cd = mb.Entity<ComparacionDetalle>();
            cd.HasKey(x => new { x.IdComparacion, x.IdDiseno });
            cd.HasRequired(x => x.Comparacion).WithMany(c => c.Detalles).HasForeignKey(x => x.IdComparacion).WillCascadeOnDelete(true);
            cd.HasRequired(x => x.Diseno).WithMany().HasForeignKey(x => x.IdDiseno);
            cd.Property(x => x.Puntaje).HasPrecision(6, 2);
        }
    }
}
