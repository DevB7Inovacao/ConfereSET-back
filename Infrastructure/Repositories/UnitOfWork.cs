namespace Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContextClass _dbContext;

        public IUserRepository Users { get; }
        public IEmpresasRepository Empresas { get; }
        public IObrasRepository Obras { get; }
        public IGrupoDeObrasRepository GrupoDeObras { get; }
        public IModeloTextoRepository ModeloTextos { get; }
        public IModeloTextoVariavelRepository ModeloTextoVariaveis { get; }
        public IModeloTextoVariavelVinculoRepository ModeloTextoVariavelVinculos { get; }
        public IMaoDeObraRepository MaoDeObra { get; }
        public IEquipamentosRepository Equipamentos { get; }
        public ITiposOcorrenciaRepository TiposOcorrencia { get; }
        public IDespesasRepository Despesas { get; set; }
        public ISupportTicketsRepository SupportTickets { get; }
        public IChecklistRepository Checklists { get; }
        public IObraOperadorRepository ObraOperadores { get; }
        public IObraMaoDeObraRepository ObraMaoDeObra { get; }
        public IObraEquipamentoRepository ObraEquipamentos { get; }
        public IObraTipoOcorrenciaRepository ObraTiposOcorrencia { get; }
        public IObraModeloTextoRepository ObraModelosTexto { get; }
        public IObraDespesaRepository ObraDespesas { get; }
        public IRelatorioRepository Relatorios { get; }
        public IOcorrenciaRepository Ocorrencias { get; }
        public IChecklistItemRepository ChecklistItems { get; }
        public IObraChecklistRepository ObraChecklists { get; }
        public IObraChecklistItemRepository ObraChecklistItems { get; }
        public IAtividadeRecenteRepository AtividadesRecentes { get; }

        public UnitOfWork(DbContextClass dbContext,
            IUserRepository userRepository,
            IEmpresasRepository empresasRepository,
            IObrasRepository obrasRepository,
            IGrupoDeObrasRepository grupoDeObrasRepository,
            IModeloTextoRepository modeloTextoRepository,
            IModeloTextoVariavelRepository modeloTextoVariavelRepository,
            IModeloTextoVariavelVinculoRepository modeloTextoVariavelVinculoRepository,
            IMaoDeObraRepository maoDeObraRepository,
            IEquipamentosRepository equipamentosRepository,
            ITiposOcorrenciaRepository tiposOcorrenciaRepository,
            IDespesasRepository despesasRepository,
            ISupportTicketsRepository supportTicketsRepository,
            IChecklistRepository checklistRepository,
            IObraOperadorRepository obraOperadorRepository,
            IObraMaoDeObraRepository obraMaoDeObraRepository,
            IObraEquipamentoRepository obraEquipamentoRepository,
            IObraTipoOcorrenciaRepository obraTipoOcorrenciaRepository,
            IObraModeloTextoRepository obraModeloTextoRepository,
            IObraDespesaRepository obraDespesaRepository,
            IRelatorioRepository relatorioRepository,
            IOcorrenciaRepository ocorrenciaRepository,
            IChecklistItemRepository checklistItemRepository,
            IObraChecklistRepository obraChecklistRepository,
            IObraChecklistItemRepository obraChecklistItemRepository,
            IAtividadeRecenteRepository atividadeRecenteRepository
        )
        {
            _dbContext = dbContext;
            Users = userRepository;
            Empresas = empresasRepository;
            Obras = obrasRepository;
            GrupoDeObras = grupoDeObrasRepository;
            ModeloTextos = modeloTextoRepository;
            ModeloTextoVariaveis = modeloTextoVariavelRepository;
            ModeloTextoVariavelVinculos = modeloTextoVariavelVinculoRepository;
            MaoDeObra = maoDeObraRepository;
            Equipamentos = equipamentosRepository;
            TiposOcorrencia = tiposOcorrenciaRepository;
            Despesas = despesasRepository;
            SupportTickets = supportTicketsRepository;
            Checklists = checklistRepository;
            ObraOperadores = obraOperadorRepository;
            ObraMaoDeObra = obraMaoDeObraRepository;
            ObraEquipamentos = obraEquipamentoRepository;
            ObraTiposOcorrencia = obraTipoOcorrenciaRepository;
            ObraModelosTexto = obraModeloTextoRepository;
            ObraDespesas = obraDespesaRepository;
            Relatorios = relatorioRepository;
            Ocorrencias = ocorrenciaRepository;
            ChecklistItems = checklistItemRepository;
            ObraChecklists = obraChecklistRepository;
            ObraChecklistItems = obraChecklistItemRepository;
            AtividadesRecentes = atividadeRecenteRepository;
        }

        public int Save() => _dbContext.SaveChanges();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) _dbContext.Dispose();
        }
    }

    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }
        IEmpresasRepository Empresas { get; }
        IObrasRepository Obras { get; }
        IGrupoDeObrasRepository GrupoDeObras { get; }
        IModeloTextoRepository ModeloTextos { get; }
        IModeloTextoVariavelRepository ModeloTextoVariaveis { get; }
        IModeloTextoVariavelVinculoRepository ModeloTextoVariavelVinculos { get; }
        IMaoDeObraRepository MaoDeObra { get; }
        IEquipamentosRepository Equipamentos { get; }
        ITiposOcorrenciaRepository TiposOcorrencia { get; }
        IDespesasRepository Despesas { get; }
        ISupportTicketsRepository SupportTickets { get; }
        IChecklistRepository Checklists { get; }
        IObraOperadorRepository ObraOperadores { get; }
        IObraMaoDeObraRepository ObraMaoDeObra { get; }
        IObraEquipamentoRepository ObraEquipamentos { get; }
        IObraTipoOcorrenciaRepository ObraTiposOcorrencia { get; }
        IObraModeloTextoRepository ObraModelosTexto { get; }
        IObraDespesaRepository ObraDespesas { get; }
        IRelatorioRepository Relatorios { get; }
        IOcorrenciaRepository Ocorrencias { get; }
        IChecklistItemRepository ChecklistItems { get; }
        IObraChecklistRepository ObraChecklists { get; }
        IObraChecklistItemRepository ObraChecklistItems { get; }
        IAtividadeRecenteRepository AtividadesRecentes { get; }
        int Save();
    }
}