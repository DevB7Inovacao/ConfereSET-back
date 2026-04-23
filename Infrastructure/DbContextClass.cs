using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
	public class DbContextClass : DbContext
	{
		public DbContextClass(DbContextOptions<DbContextClass> contextOptions) : base(contextOptions) { }

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
		public DbSet<ObraOperador> ObraOperadores { get; set; }
		public DbSet<ObraMaoDeObra> ObraMaoDeObra { get; set; }
		public DbSet<ObraEquipamento> ObraEquipamentos { get; set; }
		public DbSet<ObraTipoOcorrencia> ObraTiposOcorrencia { get; set; }
		public DbSet<ObraModeloTexto> ObraModelosTexto { get; set; }
		public DbSet<ObraDespesa> ObraDespesas { get; set; }
		public DbSet<Relatorio> Relatorios { get; set; }
		public DbSet<RelatorioSecao> RelatorioSecoes { get; set; }
		public DbSet<RelatorioSecaoItem> RelatorioSecaoItens { get; set; }
		public DbSet<RelatorioItemFoto> RelatorioItemFotos { get; set; }
		public DbSet<Ocorrencia> Ocorrencias { get; set; }
		public DbSet<RelatorioComentario> RelatorioComentarios { get; set; }
		public DbSet<ChecklistItem> ChecklistItens { get; set; }
		public DbSet<ObraChecklist> ObraChecklists { get; set; }
		public DbSet<ObraChecklistItem> ObraChecklistItens { get; set; }
		public DbSet<AtividadeRecente> AtividadesRecentes { get; set; }
		public DbSet<Plano> Planos { get; set; }
		public DbSet<Assinatura> Assinaturas { get; set; }
		public DbSet<PagamentoAssinatura> PagamentosAssinatura { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(k => k.Id);

				entity.HasOne(a => a.Empresa)
				.WithMany(s => s.Users)
				.HasForeignKey(a => a.EmpresaId);
			});

			modelBuilder.Entity<Plano>(entity =>
			{
				entity.HasKey(k => k.Id);

				entity.HasOne(a => a.Empresa)
				.WithMany(s => s.Planos)
				.HasForeignKey(a => a.EmpresaId);
			});

			modelBuilder.Entity<Empresas>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<Obras>(entity => { entity.HasKey(k => k.Id); });

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
				entity.HasOne(x => x.Group).WithMany(g => g.Obras).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.Obra).WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ModeloTexto>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<ModeloTextoVariavel>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<ModeloTextoVariavelVinculo>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<MaoDeObra>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<Equipamentos>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<TiposOcorrencia>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<Despesas>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<SupportTicket>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<Checklist>(entity => { entity.HasKey(k => k.Id); });
			modelBuilder.Entity<ObraOperador>(entity => { entity.HasKey(k => k.Id); });

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

			modelBuilder.Entity<Relatorio>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.ModeloTexto).WithMany().HasForeignKey(x => x.ModeloTextoId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(x => x.Obra).WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(x => x.CriadoPor).WithMany().HasForeignKey(x => x.CriadoPorUserId).OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<RelatorioSecao>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.Relatorio)
									.WithMany(r => r.Secoes)
									.HasForeignKey(x => x.RelatorioId)
									.OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.TipoOcorrencia)
									.WithMany()
									.HasForeignKey(x => x.TipoOcorrenciaId)
									.OnDelete(DeleteBehavior.SetNull)
									.IsRequired(false);
			});

			modelBuilder.Entity<RelatorioSecaoItem>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.RelatorioSecao)
									.WithMany(s => s.Itens)
									.HasForeignKey(x => x.RelatorioSecaoId)
									.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<RelatorioItemFoto>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.RelatorioSecaoItem)
									.WithMany(i => i.Fotos)
									.HasForeignKey(x => x.RelatorioSecaoItemId)
									.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<Ocorrencia>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.Obra).WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(x => x.TipoOcorrencia).WithMany().HasForeignKey(x => x.TipoOcorrenciaId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(x => x.CriadoPor).WithMany().HasForeignKey(x => x.CriadoPorUserId).OnDelete(DeleteBehavior.SetNull);
			});

			modelBuilder.Entity<RelatorioComentario>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.RelatorioSecao)
									.WithMany(s => s.Comentarios)
									.HasForeignKey(x => x.RelatorioSecaoId)
									.OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.Autor)
									.WithMany()
									.HasForeignKey(x => x.AutorId)
									.OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<ChecklistItem>(entity => { entity.HasKey(k => k.Id); });

			modelBuilder.Entity<ObraChecklist>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasIndex(x => new { x.ObraId, x.ChecklistId }).IsUnique();
				entity.HasOne(x => x.Obra).WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.Checklist).WithMany().HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity<ObraChecklistItem>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.ObraChecklist).WithMany(o => o.Itens).HasForeignKey(x => x.ObraChecklistId).OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.ChecklistItem).WithMany().HasForeignKey(x => x.ChecklistItemId).OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<AtividadeRecente>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.Operador).WithMany().HasForeignKey(x => x.OperadorId).OnDelete(DeleteBehavior.Cascade);
				entity.HasOne(x => x.Obra).WithMany().HasForeignKey(x => x.ObraId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
			});

			modelBuilder.Entity<Plano>(entity => { entity.HasKey(k => k.Id); });

			modelBuilder.Entity<Assinatura>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
				entity.HasOne(x => x.Plano).WithMany().HasForeignKey(x => x.PlanoId).OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<PagamentoAssinatura>(entity =>
			{
				entity.HasKey(k => k.Id);
				entity.HasOne(x => x.Assinatura).WithMany(a => a.Pagamentos).HasForeignKey(x => x.AssinaturaId).OnDelete(DeleteBehavior.Cascade);
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
					((BaseModel)entityEntry.Entity).CreatedDate = DateTime.Now;
			}
			return base.SaveChanges();
		}
	}
}