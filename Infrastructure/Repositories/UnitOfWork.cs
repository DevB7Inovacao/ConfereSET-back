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

        public UnitOfWork(DbContextClass dbContext,
                    IUserRepository userRepository,
                    IEmpresasRepository empresasRepository,
                    IObrasRepository obrasRepository,
                    IGrupoDeObrasRepository grupoDeObrasRepository
        )
        {
            _dbContext = dbContext;

            this.Users = userRepository;
            this.Empresas = empresasRepository;
            this.Obras = obrasRepository;
            this.GrupoDeObras = grupoDeObrasRepository;
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
        int Save();
    }
}
