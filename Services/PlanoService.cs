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
                MPPreapprovalPlanId = null,
                EmpresaId=req.EmpresaId
                
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

        public async Task<List<PlanoDTO>> GetAll(int empresaid)
        {
            var planos = await _unitOfWork.Planos.GetAll(empresaid);
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

        /// <summary>
        /// Exclui um plano. Se houver QUALQUER assinatura vinculada a ele, não é possível
        /// excluir (FK) — nesse caso o plano é apenas DESATIVADO (Ativo=false), saindo da
        /// listagem pública mas preservando o histórico das assinaturas.
        /// </summary>
        public async Task<PlanoDeleteResult> Delete(int id)
        {
            var plano = await _unitOfWork.Planos.GetPlanoById(id)
                ?? throw new Exception("Plano não encontrado.");

            var emUso = await _unitOfWork.Assinaturas.CountByPlanoId(id);

            if (emUso > 0)
            {
                // Há empresas usando o plano → desativa em vez de excluir.
                plano.Ativo = false;

                if (!string.IsNullOrWhiteSpace(plano.MPPreapprovalPlanId))
                {
                    try
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
                            Status = "inactive"
                        });
                    }
                    catch { /* sincronização com o MP é best-effort */ }
                }

                _unitOfWork.Planos.Update(plano);
                _unitOfWork.Save();

                return new PlanoDeleteResult
                {
                    Excluido = false,
                    Desativado = true,
                    Mensagem = $"O plano está em uso por {emUso} assinatura(s); foi desativado em vez de excluído."
                };
            }

            _unitOfWork.Planos.Delete(plano);
            _unitOfWork.Save();

            return new PlanoDeleteResult
            {
                Excluido = true,
                Desativado = false,
                Mensagem = "Plano excluído com sucesso."
            };
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
            MPPreapprovalPlanId = p.MPPreapprovalPlanId,
            EmpresaId = p.EmpresaId
        };
    }

    public class PlanoDeleteResult
    {
        public bool Excluido { get; set; }
        public bool Desativado { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public interface IPlanoService
    {
        Task<PlanoDTO> Create(CreatePlanoRequest req);
        Task<PlanoDTO> Update(int id, UpdatePlanoRequest req);
        Task<List<PlanoDTO>> GetAll(int empresaid);
        Task<List<PlanoDTO>> GetAtivos();
        Task<PlanoDTO?> GetById(int id);
        Task<PlanoDeleteResult> Delete(int id);
    }
}