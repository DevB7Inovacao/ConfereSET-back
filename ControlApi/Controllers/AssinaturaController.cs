using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Gestão de assinaturas (planos pagos via Mercado Pago).
	/// <para>
	/// Regras de autorização:
	/// <list type="bullet">
	/// <item><b>checkout / cancelar / atualizar / get*</b>: usuário autenticado da própria empresa.
	/// Aprovação/edição de plano fica restrita a admin/gerente.</item>
	/// <item><b>vitalicio</b>: apenas usuários do tipo <c>admin</c>. Útil para o "dono da plataforma"
	/// atribuir manualmente um plano a uma empresa parceira.</item>
	/// <item><b>callback</b>: anônimo. É chamado pelo navegador do usuário após retornar do
	/// Mercado Pago — neste momento pode não haver token na requisição.</item>
	/// </list>
	/// </para>
	/// </summary>
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class AssinaturaController : ControllerBase
	{
		private readonly IAssinaturaService _assinaturaService;

		public AssinaturaController(IAssinaturaService assinaturaService)
		{
			_assinaturaService = assinaturaService;
		}

		/// <summary>
		/// Inicia o checkout de uma assinatura. O usuário é redirecionado ao Mercado Pago após
		/// receber o <c>InitPoint</c>.
		/// </summary>
		[HttpPost("checkout")]
		public async Task<IActionResult> IniciarCheckout([FromBody] CreateAssinaturaRequest req)
		{
			try
			{
				if (req == null) return BadRequest("Payload inválido.");

				var empresaJwt = User.GetEmpresaId();
				// EmpresaId vem sempre do JWT — body é ignorado por segurança.
				req.EmpresaId = empresaJwt;

				var result = await _assinaturaService.IniciarCheckout(req);
				return Ok(result);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Endpoint anônimo de retorno do Mercado Pago. Chamado pelo navegador do usuário após
		/// completar o fluxo de pagamento, possivelmente sem cookie/token. Apenas consulta o status
		/// do preapproval no MP e devolve uma mensagem amigável — não altera estado autoritativo
		/// (isso é responsabilidade do webhook autenticado).
		/// </summary>
		[AllowAnonymous]
		[HttpPost("callback")]
		public async Task<IActionResult> Callback([FromQuery] string preapproval_id)
		{
			try
			{
				var result = await _assinaturaService.CallBack(preapproval_id);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Atualiza valor/cartão da assinatura ativa da própria empresa.
		/// </summary>
		[HttpPut("{id}/atualizar")]
		public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarAssinaturaRequest req)
		{
			try
			{
				if (id <= 0) return BadRequest("id inválido.");
				if (req == null) return BadRequest("Payload inválido.");

				var empresaJwt = User.GetEmpresaId();
				if (!User.IsAdminOrGerente())
					return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode alterar a assinatura.");

				var assinatura = await _assinaturaService.GetById(id);
				if (assinatura == null) return NotFound("Assinatura não encontrada.");
				if (assinatura.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Assinatura pertence a outra empresa.");

				var result = await _assinaturaService.AtualizarAssinatura(id, req.NovoValor, req.CardToken);
				return result ? Ok(true) : BadRequest("Falha ao atualizar assinatura.");
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Atribui um plano vitalício a uma empresa. Operação privilegiada: só admin do dono da
		/// plataforma deve poder executar (em produção, restringir ainda mais por IP/segredo).
		/// </summary>
		[HttpPost("vitalicio")]
		public async Task<IActionResult> AtribuirVitalicio([FromBody] AtribuirPlanoVitalicioRequest req)
		{
			try
			{
				if (req == null) return BadRequest("Payload inválido.");
				if (User.GetUserType() != Core.Models.TypeUser.admin)
					return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin pode atribuir plano vitalício.");

				var result = await _assinaturaService.AtribuirPlanoVitalicio(req.EmpresaId, req.PlanoId);
				return Ok(result);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>Assinatura ativa da empresa. EmpresaId é forçado pelo JWT.</summary>
		[HttpGet("empresa/{empresaId}")]
		public async Task<IActionResult> GetByEmpresaId(int empresaId)
		{
			try
			{
				var empresaJwt = User.GetEmpresaId();
				if (empresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				var assinatura = await _assinaturaService.GetByEmpresaId(empresaId);
				if (assinatura == null) return NotFound("Nenhuma assinatura ativa encontrada.");
				return Ok(assinatura);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				var empresaJwt = User.GetEmpresaId();
				var assinatura = await _assinaturaService.GetById(id);
				if (assinatura == null) return NotFound("Assinatura não encontrada.");
				if (assinatura.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Assinatura pertence a outra empresa.");
				return Ok(assinatura);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		[HttpPost("{id}/cancelar")]
		public async Task<IActionResult> Cancelar(int id)
		{
			try
			{
				if (id <= 0) return BadRequest("id inválido.");

				var empresaJwt = User.GetEmpresaId();
				if (!User.IsAdminOrGerente())
					return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode cancelar a assinatura.");

				var assinatura = await _assinaturaService.GetById(id);
				if (assinatura == null) return NotFound("Assinatura não encontrada.");
				if (assinatura.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Assinatura pertence a outra empresa.");

				var result = await _assinaturaService.Cancelar(id);
				return result ? Ok(true) : BadRequest("Falha ao cancelar assinatura.");
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("all")]
		public async Task<IActionResult> GetAllPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			int empresaId = User.GetEmpresaId();
			var (items, total) = await _assinaturaService.GetAllPaged(page, pageSize, empresaId);
			return Ok(new { items, total });
		}

		[HttpGet("empresa/{empresaId}/limites")]
		public async Task<IActionResult> VerificarLimites(int empresaId)
		{
			try
			{
				var empresaJwt = User.GetEmpresaId();
				if (empresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				var limites = await _assinaturaService.VerificarLimites(empresaId);
				return Ok(limites);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}
	}
}