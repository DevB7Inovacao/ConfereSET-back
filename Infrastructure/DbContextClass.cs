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