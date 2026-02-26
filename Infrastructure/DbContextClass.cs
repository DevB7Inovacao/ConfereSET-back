using Core.Models;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
	public class DbContextClass : DbContext
	{
		public DbContextClass(DbContextOptions<DbContextClass> contextOptions) : base(contextOptions)
		{ }

		public DbSet<User> User { get; set; }
		public DbSet<Empresas> Empresas { get; set; }
		public DbSet<Obras> Obras { get; set; }
        public DbSet<GrupoDeObras> GrupoDeObras { get; set; }
        public DbSet<RelacaoGrupoObras> RelacaoGrupoObras { get; set; }
        public DbSet<ModeloTexto> ModeloTextos { get; set; }
        public DbSet<ModeloTextoVariavel> ModeloTextoVariaveis { get; set; }
        public DbSet<ModeloTextoVariavelVinculo> ModeloTextoVariavelVinculos { get; set; }
		public DbSet<MaoDeObra> MaoDeObra { get; set; }
		public DbSet<Equipamentos> Equipamentos { get; set; }
		public DbSet<TiposOcorrencia> TiposOcorrencia { get; set; }
		public DbSet<Despesas> Despesas { get; set; }
		public DbSet<SupportTicket> SupportTickets { get; set; }
		public DbSet<Checklist> Checklists { get; set; }
		public DbSet<ChecklistVariavel> ChecklistVariaveis { get; set; }
		public DbSet<ObraOperador> ObraOperadores { get; set; }
        public DbSet<ObraMaoDeObra> ObraMaoDeObra { get; set; }
        public DbSet<ObraEquipamento> ObraEquipamentos { get; set; }
        public DbSet<ObraTipoOcorrencia> ObraTiposOcorrencia { get; set; }
        public DbSet<ObraModeloTexto> ObraModelosTexto { get; set; }
        public DbSet<ObraDespesa> ObraDespesas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<Empresas>(entity =>
			{
                entity.HasKey(k => k.Id);
            });

			modelBuilder.Entity<Obras>(entity =>
			{
				entity.HasKey(k => k.Id);
			});

            modelBuilder.Entity<GrupoDeObras>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.Property(x => x.Name).IsRequired();
                entity.Property(x => x.Status).IsRequired();
            });

            modelBuilder.Entity<RelacaoGrupoObras>(entity =>
            {
                entity.HasKey(k => k.Id);

                entity.HasIndex(x => new { x.GroupId, x.ObraId }).IsUnique();

                entity.HasOne(x => x.Group)
                    .WithMany(g => g.Obras)
                    .HasForeignKey(x => x.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Obra)
                    .WithMany()
                    .HasForeignKey(x => x.ObraId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

			modelBuilder.Entity<ModeloTexto>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<ModeloTextoVariavel>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<ModeloTextoVariavelVinculo>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<MaoDeObra>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<Equipamentos>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<TiposOcorrencia>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<Despesas>(entity =>
			{
				entity.HasKey(k => k.Id);
            });

			modelBuilder.Entity<SupportTicket>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<Checklist>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<ChecklistVariavel>(entity =>
            {
				entity.HasKey(k => k.Id);
			});

			modelBuilder.Entity<ObraOperador>(entity =>
			{
				entity.HasKey(k => k.Id);
			});

            modelBuilder.Entity<ObraMaoDeObra>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.HasIndex(x => new { x.ObraId, x.MaoDeObraId }).IsUnique();
            });

            modelBuilder.Entity<ObraEquipamento>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.HasIndex(x => new { x.ObraId, x.EquipamentoId }).IsUnique();
            });

			modelBuilder.Entity<ObraTipoOcorrencia>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasIndex(x => new { x.ObraId, x.TipoOcorrenciaId }).IsUnique();
			});

            modelBuilder.Entity<ObraModeloTexto>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.HasIndex(x => new { x.ObraId, x.ModeloTextoId }).IsUnique();
            });

            modelBuilder.Entity<ObraDespesa>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.HasIndex(x => new { x.ObraId, x.DespesaId }).IsUnique();
            });

            base.OnModelCreating(modelBuilder);
		}

		public override int SaveChanges()
		{
			var entries = ChangeTracker
		.Entries()
		.Where(e => e.Entity is BaseModel && (
						e.State == EntityState.Added
						|| e.State == EntityState.Modified));

			foreach (var entityEntry in entries)
			{
				((BaseModel)entityEntry.Entity).UpdatedDate = DateTime.Now;

				if (entityEntry.State == EntityState.Added)
				{
					((BaseModel)entityEntry.Entity).CreatedDate = DateTime.Now;
				}
			}
			return base.SaveChanges();
		}
	}
}