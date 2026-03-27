using Core.DTO;
using Core.Models;
using Infrastructure.MercadoPago;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public class PlanoService : IPlanoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMercadoPagoClient _mpClient;
        private readonly string _backUrl;

        public PlanoService(IUnitOfWork unitOfWork, IMercadoPagoClient mpClient, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mpClient = mpClient;
            _backUrl = configuration["MercadoPago:BackUrl"]
                ?? throw new InvalidOperationException("MercadoPago:BackUrl não configurado.");
        }

        public async Task<PlanoDTO> Create(CreatePlanoRequest req)
        {
            var plano = new Plano
            {
                Nome = req.Nome,
                Descricao = req.Descricao,
                Valor = req.Valor,
                Recorrencia = req.Recorrencia,
                LimiteGestores = req.LimiteGestores,
                LimiteOperadores = req.LimiteOperadores,
                Ativo = true,
                MPPreapprovalPlanId = null
            };

            if (req.Valor > 0)
            {
                var mpPlan = await _mpClient.CreatePreapprovalPlan(new MPCreatePreapprovalPlanRequest
                {
                    Reason = req.Nome,
                    BackUrl = _backUrl,
                    AutoRecurring = new MPAutoRecurring
                    {
                        Frequency = (int)req.Recorrencia,
                        FrequencyType = "months",
                        TransactionAmount = req.Valor,
                        CurrencyId = "BRL"
                    }
                });
                plano.MPPreapprovalPlanId = mpPlan.Id;
            }

            await _unitOfWork.Planos.Add(plano);
            _unitOfWork.Save();

            return MapToDTO(plano);
        }

        public async Task<PlanoDTO> Update(int id, UpdatePlanoRequest req)
        {
            var plano = await _unitOfWork.Planos.GetPlanoById(id)
                ?? throw new Exception("Plano não encontrado.");

            if (req.Nome != null) plano.Nome = req.Nome;
            if (req.Descricao != null) plano.Descricao = req.Descricao;
            if (req.Valor.HasValue) plano.Valor = req.Valor.Value;
            if (req.Recorrencia.HasValue) plano.Recorrencia = req.Recorrencia.Value;
            if (req.LimiteGestores.HasValue) plano.LimiteGestores = req.LimiteGestores.Value;
            if (req.LimiteOperadores.HasValue) plano.LimiteOperadores = req.LimiteOperadores.Value;
            if (req.Ativo.HasValue) plano.Ativo = req.Ativo.Value;

            if (!string.IsNullOrWhiteSpace(plano.MPPreapprovalPlanId))
            {
                await _mpClient.UpdatePreapprovalPlan(plano.MPPreapprovalPlanId, new MPUpdatePreapprovalPlanRequest
                {
                    Reason = plano.Nome,
                    AutoRecurring = new MPAutoRecurring
                    {
                        Frequency = (int)plano.Recorrencia,
                        FrequencyType = "months",
                        TransactionAmount = plano.Valor,
                        CurrencyId = "BRL"
                    },
                    Status = plano.Ativo ? "active" : "inactive"
                });
            }

            _unitOfWork.Planos.Update(plano);
            _unitOfWork.Save();

            return MapToDTO(plano);
        }

        public async Task<List<PlanoDTO>> GetAll()
        {
            var planos = await _unitOfWork.Planos.GetAll();
            return planos.Select(MapToDTO).ToList();
        }

        public async Task<List<PlanoDTO>> GetAtivos()
        {
            var planos = await _unitOfWork.Planos.GetAllAtivos();
            return planos.Select(MapToDTO).ToList();
        }

        public async Task<PlanoDTO?> GetById(int id)
        {
            var plano = await _unitOfWork.Planos.GetPlanoById(id);
            return plano == null ? null : MapToDTO(plano);
        }

        private static PlanoDTO MapToDTO(Plano p) => new()
        {
            Id = p.Id,
            Nome = p.Nome,
            Descricao = p.Descricao,
            Valor = p.Valor,
            Recorrencia = p.Recorrencia,
            LimiteGestores = p.LimiteGestores,
            LimiteOperadores = p.LimiteOperadores,
            Ativo = p.Ativo,
            MPPreapprovalPlanId = p.MPPreapprovalPlanId
        };
    }

    public interface IPlanoService
    {
        Task<PlanoDTO> Create(CreatePlanoRequest req);
        Task<PlanoDTO> Update(int id, UpdatePlanoRequest req);
        Task<List<PlanoDTO>> GetAll();
        Task<List<PlanoDTO>> GetAtivos();
        Task<PlanoDTO?> GetById(int id);
    }
}