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