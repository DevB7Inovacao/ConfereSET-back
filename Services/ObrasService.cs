using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
    public class ObrasService : IObrasService
    {
        public IUnitOfWork _unitOfWork;

        public ObrasService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Obras> CreateObra(Obras obras)
        {
            try
            {
                if (obras == null)
                    throw new ArgumentNullException(nameof(obras));
                await _unitOfWork.Obras.Add(obras);
                _unitOfWork.Save();
                return obras;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> UpdateObra(Obras obra, int idObra)
        {
            var result = _unitOfWork.Save();
            return result > 0;
        }

        public async Task<bool> DeleteObra(int obraId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                {
                    throw new Exception("Obra não encontrada.");
                }
                _unitOfWork.Obras.Delete(obra);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao excluir a obra: " + ex.Message);
            }
        }

        public async Task<bool> ToggleObraStatus(int obraId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                {
                    throw new Exception("Obra não encontrada.");
                }

                obra.Status = obra.Status == 1 ? 0 : 1;

                _unitOfWork.Obras.Update(obra);
                var result = _unitOfWork.Save();

                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Falha ao alterar o status da obra: " + ex.Message);
            }
        }

        public async Task<Obras> GetObraById(int id)
        {
            return await _unitOfWork.Obras.GetObraById(id);
        }

        public async Task<ObrasPagedDTO> GetObrasPaged(FiltersObrasDTO filtersDTO)
        {
            try
            {
                var obras = await _unitOfWork.Obras.GetAllObrasPaged(filtersDTO);

                if (obras == null || obras.Results == null || !obras.Results.Any())
                {
                    throw new Exception("Nenhum dado foi encontrado.");
                }

                var obraIds = obras.Results.Select(o => o.Id).ToList();
                var operadoresCounts = await GetOperadoresCountsByObraIds(obraIds);

                var obraDTO = obras.Results.Select(obra => new ObrasDTO
                {
                    Id = obra.Id,
                    Name = obra.Name,
                    Status = obra.Status,
                    StreetAddress = obra.StreetAddress,
                    Number = obra.Number,
                    AddressLine2 = obra.AddressLine2,
                    Neighborhood = obra.Neighborhood,
                    City = obra.City,
                    State = obra.State,
                    PostalCode = obra.PostalCode,
                    Country = obra.Country,
                    ClientName = obra.ClientName,
                    ClientEmail = obra.ClientEmail,
                    ClientPhone = obra.ClientPhone,
                    ClientDocument = obra.ClientDocument,
                    EmpresaId = obra.EmpresaId,
                    StartDate = obra.StartDate,
                    ProgressPercentage = obra.ProgressPercentage,
                    OperadoresCount = operadoresCounts.ContainsKey(obra.Id) ? operadoresCounts[obra.Id] : 0
                }).ToList();

                return new ObrasPagedDTO() { Result = obraDTO, PageCount = obras.PageCount };
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraSimpleDTO>> GetObrasSimple()
        {
            var list = await _unitOfWork.Obras.GetObrasSimple();
            return list;
        }

        public async Task<bool> AddOperadorToObra(int obraId, int operadorId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var operador = await _unitOfWork.Users.GetById(operadorId);
                if (operador == null)
                    throw new Exception("Operador não encontrado.");

                if (operador.Type != 3)
                    throw new Exception("Usuário não é um operador.");

                var added = await _unitOfWork.ObraOperadores.AddOperadorToObra(obraId, operadorId);
                if (!added)
                    throw new Exception("Operador já está vinculado a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveOperadorFromObra(int obraId, int operadorId)
        {
            try
            {
                var removed = await _unitOfWork.ObraOperadores.RemoveOperadorFromObra(obraId, operadorId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId)
        {
            return await _unitOfWork.ObraOperadores.GetOperadoresByObraId(obraId);
        }

        public async Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId)
        {
            return await _unitOfWork.ObraOperadores.GetObrasByOperadorId(operadorId);
        }

        public async Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId)
        {
            return await _unitOfWork.ObraOperadores.GetObraWithOperadores(obraId);
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId)
        {
            return await _unitOfWork.Obras.GetObrasCardsByEmpresaId(empresaId);
        }

        public async Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId)
        {
            return await _unitOfWork.Obras.GetObrasCardsByOperadorId(operadorId);
        }

        public async Task<bool> AddMaoDeObraToObra(int obraId, int maoDeObraId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var maoDeObra = await _unitOfWork.MaoDeObra.GetById(maoDeObraId);
                if (maoDeObra == null)
                    throw new Exception("Mão de obra não encontrada.");

                var added = await _unitOfWork.ObraMaoDeObra.AddMaoDeObraToObra(obraId, maoDeObraId);
                if (!added)
                    throw new Exception("Mão de obra já está vinculada a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveMaoDeObraFromObra(int obraId, int maoDeObraId)
        {
            try
            {
                var removed = await _unitOfWork.ObraMaoDeObra.RemoveMaoDeObraFromObra(obraId, maoDeObraId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraMaoDeObraDTO>> GetMaoDeObraByObraId(int obraId)
        {
            return await _unitOfWork.ObraMaoDeObra.GetMaoDeObraByObraId(obraId);
        }

        public async Task<bool> AddEquipamentoToObra(int obraId, int equipamentoId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var equipamento = await _unitOfWork.Equipamentos.GetById(equipamentoId);
                if (equipamento == null)
                    throw new Exception("Equipamento não encontrado.");

                var added = await _unitOfWork.ObraEquipamentos.AddEquipamentoToObra(obraId, equipamentoId);
                if (!added)
                    throw new Exception("Equipamento já está vinculado a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveEquipamentoFromObra(int obraId, int equipamentoId)
        {
            try
            {
                var removed = await _unitOfWork.ObraEquipamentos.RemoveEquipamentoFromObra(obraId, equipamentoId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraEquipamentoDTO>> GetEquipamentosByObraId(int obraId)
        {
            return await _unitOfWork.ObraEquipamentos.GetEquipamentosByObraId(obraId);
        }

        public async Task<bool> AddTipoOcorrenciaToObra(int obraId, int tipoOcorrenciaId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var tipoOcorrencia = await _unitOfWork.TiposOcorrencia.GetById(tipoOcorrenciaId);
                if (tipoOcorrencia == null)
                    throw new Exception("Tipo de ocorrência não encontrado.");

                var added = await _unitOfWork.ObraTiposOcorrencia.AddTipoOcorrenciaToObra(obraId, tipoOcorrenciaId);
                if (!added)
                    throw new Exception("Tipo de ocorrência já está vinculado a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveTipoOcorrenciaFromObra(int obraId, int tipoOcorrenciaId)
        {
            try
            {
                var removed = await _unitOfWork.ObraTiposOcorrencia.RemoveTipoOcorrenciaFromObra(obraId, tipoOcorrenciaId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraTipoOcorrenciaDTO>> GetTiposOcorrenciaByObraId(int obraId)
        {
            return await _unitOfWork.ObraTiposOcorrencia.GetTiposOcorrenciaByObraId(obraId);
        }

        public async Task<bool> AddModeloTextoToObra(int obraId, int modeloTextoId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var modeloTexto = await _unitOfWork.ModeloTextos.GetById(modeloTextoId);
                if (modeloTexto == null)
                    throw new Exception("Modelo de texto não encontrado.");

                var added = await _unitOfWork.ObraModelosTexto.AddModeloTextoToObra(obraId, modeloTextoId);
                if (!added)
                    throw new Exception("Modelo de texto já está vinculado a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveModeloTextoFromObra(int obraId, int modeloTextoId)
        {
            try
            {
                var removed = await _unitOfWork.ObraModelosTexto.RemoveModeloTextoFromObra(obraId, modeloTextoId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraModeloTextoDTO>> GetModelosTextoByObraId(int obraId)
        {
            return await _unitOfWork.ObraModelosTexto.GetModelosTextoByObraId(obraId);
        }

        public async Task<bool> AddDespesaToObra(int obraId, int despesaId)
        {
            try
            {
                var obra = await _unitOfWork.Obras.GetObraById(obraId);
                if (obra == null)
                    throw new Exception("Obra não encontrada.");

                var despesa = await _unitOfWork.Despesas.GetById(despesaId);
                if (despesa == null)
                    throw new Exception("Despesa não encontrada.");

                var added = await _unitOfWork.ObraDespesas.AddDespesaToObra(obraId, despesaId);
                if (!added)
                    throw new Exception("Despesa já está vinculada a esta obra.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> RemoveDespesaFromObra(int obraId, int despesaId)
        {
            try
            {
                var removed = await _unitOfWork.ObraDespesas.RemoveDespesaFromObra(obraId, despesaId);
                if (!removed)
                    throw new Exception("Relação não encontrada.");

                var result = _unitOfWork.Save();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ObraDespesaDTO>> GetDespesasByObraId(int obraId)
        {
            return await _unitOfWork.ObraDespesas.GetDespesasByObraId(obraId);
        }

        private async Task<Dictionary<int, int>> GetOperadoresCountsByObraIds(List<int> obraIds)
        {
            var counts = new Dictionary<int, int>();
            foreach (var obraId in obraIds)
            {
                var operadores = await _unitOfWork.ObraOperadores.GetOperadoresByObraId(obraId);
                counts[obraId] = operadores.Count;
            }
            return counts;
        }
    }

    public interface IObrasService
    {
        Task<Obras> CreateObra(Obras obras);
        Task<bool> UpdateObra(Obras obras, int idObra);
        Task<bool> DeleteObra(int obraId);
        Task<bool> ToggleObraStatus(int obraId);
        Task<Obras> GetObraById(int id);
        Task<ObrasPagedDTO?> GetObrasPaged(FiltersObrasDTO filtersDTO);
        Task<List<ObraSimpleDTO>> GetObrasSimple();
        Task<bool> AddOperadorToObra(int obraId, int operadorId);
        Task<bool> RemoveOperadorFromObra(int obraId, int operadorId);
        Task<List<ObraOperadorDTO>> GetOperadoresByObraId(int obraId);
        Task<List<ObrasDTO>> GetObrasByOperadorId(int operadorId);
        Task<ObraWithOperadoresDTO?> GetObraWithOperadores(int obraId);
        Task<List<ObraCardDTO>> GetObrasCardsByEmpresaId(int empresaId);
        Task<List<ObraCardDTO>> GetObrasCardsByOperadorId(int operadorId);
        Task<bool> AddMaoDeObraToObra(int obraId, int maoDeObraId);
        Task<bool> RemoveMaoDeObraFromObra(int obraId, int maoDeObraId);
        Task<List<ObraMaoDeObraDTO>> GetMaoDeObraByObraId(int obraId);
        Task<bool> AddEquipamentoToObra(int obraId, int equipamentoId);
        Task<bool> RemoveEquipamentoFromObra(int obraId, int equipamentoId);
        Task<List<ObraEquipamentoDTO>> GetEquipamentosByObraId(int obraId);
        Task<bool> AddTipoOcorrenciaToObra(int obraId, int tipoOcorrenciaId);
        Task<bool> RemoveTipoOcorrenciaFromObra(int obraId, int tipoOcorrenciaId);
        Task<List<ObraTipoOcorrenciaDTO>> GetTiposOcorrenciaByObraId(int obraId);
        Task<bool> AddModeloTextoToObra(int obraId, int modeloTextoId);
        Task<bool> RemoveModeloTextoFromObra(int obraId, int modeloTextoId);
        Task<List<ObraModeloTextoDTO>> GetModelosTextoByObraId(int obraId);
        Task<bool> AddDespesaToObra(int obraId, int despesaId);
        Task<bool> RemoveDespesaFromObra(int obraId, int despesaId);
        Task<List<ObraDespesaDTO>> GetDespesasByObraId(int obraId);
    }
}