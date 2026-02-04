using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    IDespesasRepository despesasRepository
        )
        {
            _dbContext = dbContext;

            this.Users = userRepository;
            this.Empresas = empresasRepository;
            this.Obras = obrasRepository;
            this.GrupoDeObras = grupoDeObrasRepository;
            this.ModeloTextos = modeloTextoRepository;
            this.ModeloTextoVariaveis = modeloTextoVariavelRepository;
            this.ModeloTextoVariavelVinculos = modeloTextoVariavelVinculoRepository;
            this.MaoDeObra = maoDeObraRepository;
            this.Equipamentos = equipamentosRepository;
            this.TiposOcorrencia = tiposOcorrenciaRepository;
            this.Despesas = despesasRepository;
        }

        public int Save()
        {
            return _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
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
        int Save();
    }
}
