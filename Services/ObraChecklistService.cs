using Core.DTO;
using Core.Models;
using Infrastructure.Repositories;

namespace Services
{
	public class ObraChecklistService : IObraChecklistService
	{
		private readonly IUnitOfWork _unitOfWork;

		public ObraChecklistService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<ObraChecklistDTO> AddChecklistToObra(AddChecklistToObraRequest req)
		{
			var obra = await _unitOfWork.Obras.GetObraById(req.ObraId);
			if (obra == null) throw new Exception("Obra não encontrada.");

			var checklist = await _unitOfWork.Checklists.GetById(req.ChecklistId);
			if (checklist == null) throw new Exception("Checklist não encontrado.");

			if (await _unitOfWork.ObraChecklists.Exists(req.ObraId, req.ChecklistId))
				throw new Exception("Checklist já está vinculado a esta obra.");

			var obraChecklist = new ObraChecklist
			{
				ObraId = req.ObraId,
				ChecklistId = req.ChecklistId,
				Status = 1
			};

			await _unitOfWork.ObraChecklists.Add(obraChecklist);
			_unitOfWork.Save();

			var itens = await _unitOfWork.ChecklistItems.GetByChecklist(req.ChecklistId);

			foreach (var item in itens.Where(i => i.Status == 1))
			{
				await _unitOfWork.ObraChecklistItems.Add(new ObraChecklistItem
				{
					ObraChecklistId = obraChecklist.Id,
					ChecklistItemId = item.Id,
					Resposta = 0
				});
			}

			_unitOfWork.Save();

			return await GetById(obraChecklist.Id);
		}

		public async Task<ObraChecklistDTO> GetById(int id)
		{
			var obraChecklist = await _unitOfWork.ObraChecklists.GetById(id);
			if (obraChecklist == null) throw new Exception("Vínculo de checklist não encontrado.");

			return MapToDTO(obraChecklist);
		}

		public async Task<List<ObraChecklistDTO>> GetByObra(int obraId)
		{
			var list = await _unitOfWork.ObraChecklists.GetByObra(obraId);
			return list.Select(MapToDTO).ToList();
		}

		public async Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req)
		{
			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			if (item == null) throw new Exception("Item não encontrado.");

			item.Resposta = req.Resposta;
			item.Observacao = req.Observacao;

			_unitOfWork.ObraChecklistItems.Update(item);
			return _unitOfWork.Save() > 0;
		}
		public async Task<bool> ResponderItensAdicionais(int obraChecklistItemId, ResponderChecklistItemRequest req)
		{
			var item = await _unitOfWork.ObraChecklistItems.GetById(obraChecklistItemId);
			if (item == null) throw new Exception("Item não encontrado.");


			item.Observacao = req.Observacao;
			item.Empresa = req.Empresa;
			item.DataHora = req.DataHora;
			item.Equipamento = req.Equipamento;
			item.Marca = req.Marca;
			_unitOfWork.ObraChecklistItems.Update(item);
			return _unitOfWork.Save() > 0;
		}

		public async Task<bool> RemoveChecklistFromObra(int obraChecklistId)
		{
			var obraChecklist = await _unitOfWork.ObraChecklists.GetById(obraChecklistId);
			if (obraChecklist == null) throw new Exception("Vínculo não encontrado.");

			var itens = await _unitOfWork.ObraChecklistItems.GetByObraChecklist(obraChecklistId);
			foreach (var item in itens)
				_unitOfWork.ObraChecklistItems.Delete(item);

			_unitOfWork.ObraChecklists.Delete(obraChecklist);
			return _unitOfWork.Save() > 0;
		}

		public async Task<bool> SincronizarChecklist(int checklistId)
		{
			var checklist = await _unitOfWork.Checklists.GetById(checklistId);
			if (checklist == null) throw new Exception("Checklist não encontrado.");

			var itensAtivos = await _unitOfWork.ChecklistItems.GetByChecklist(checklistId);
			var idsAtivos = itensAtivos.Where(i => i.Status == 1).Select(i => i.Id).ToHashSet();
			var idsTodos = itensAtivos.Select(i => i.Id).ToHashSet();

			var obraChecklists = await _unitOfWork.ObraChecklists.GetByChecklistId(checklistId);

			foreach (var obraChecklist in obraChecklists)
			{
				var itensExistentes = await _unitOfWork.ObraChecklistItems.GetByObraChecklist(obraChecklist.Id);
				var idsExistentes = itensExistentes.Select(i => i.ChecklistItemId).ToHashSet();

				foreach (var itemId in idsAtivos.Except(idsExistentes))
				{
					await _unitOfWork.ObraChecklistItems.Add(new ObraChecklistItem
					{
						ObraChecklistId = obraChecklist.Id,
						ChecklistItemId = itemId,
						Resposta = 0
					});
				}

				foreach (var item in itensExistentes.Where(i => !idsTodos.Contains(i.ChecklistItemId)))
				{
					_unitOfWork.ObraChecklistItems.Delete(item);
				}
			}

			return _unitOfWork.Save() >= 0;
		}

		private static ObraChecklistDTO MapToDTO(ObraChecklist x) => new()
		{
			Id = x.Id,
			ObraId = x.ObraId,
			ChecklistId = x.ChecklistId,
			ChecklistNome = x.Checklist?.Nome,
			Status = x.Status,
			Itens = x.Itens.Select(i => new ObraChecklistItemDTO
			{
				Id = i.Id,
				ObraChecklistId = i.ObraChecklistId,
				ChecklistItemId = i.ChecklistItemId,
				Descricao = i.ChecklistItem?.Descricao,
				Ordem = i.ChecklistItem?.Ordem ?? 0,
				Resposta = i.Resposta,
				Observacao = i.Observacao,
				Empresa = i.Empresa,
				DataHora = i.DataHora,
				Equipamento = i.Equipamento,
				Marca = i.Marca,
			}).OrderBy(i => i.Ordem).ToList()
		};
	}

	public interface IObraChecklistService
	{
		Task<ObraChecklistDTO> AddChecklistToObra(AddChecklistToObraRequest req);
		Task<ObraChecklistDTO> GetById(int id);
		Task<List<ObraChecklistDTO>> GetByObra(int obraId);
		Task<bool> ResponderItem(int obraChecklistItemId, ResponderChecklistItemRequest req);
		Task<bool> RemoveChecklistFromObra(int obraChecklistId);
		Task<bool> SincronizarChecklist(int checklistId);
		Task<bool> ResponderItensAdicionais(int obraChecklistItemId, ResponderChecklistItemRequest req);

	}
}