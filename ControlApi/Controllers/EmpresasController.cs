using AutoMapper;
using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class EmpresasController : ControllerBase
	{
		private readonly IJWTManager _jWTManager;
		private readonly IMapper _mapper;
		IEmpresasService _empresasService;

		public EmpresasController(IJWTManager jWTManager, IMapper mapper, IEmpresasService empresasService)
		{
			this._jWTManager = jWTManager;
			this._mapper = mapper;
			this._empresasService = empresasService;
		}

		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> CreateEmpresa([FromBody] EmpresasDTO empresas)
		{
			try
			{
				var empresa = _mapper.Map<Empresas>(empresas);
				var result = await _empresasService.CreateEmpresa(empresa);

				if (result.Success)
					return Ok("Empresa cadastrada com sucesso.");
				else
					return BadRequest("Erro ao cadastrar empresa.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("{empresaId}")]
		public async Task<IActionResult> UpdateById(int empresaId, [FromBody] UpdateEmpresaByIdRequest req)
		{
			if (empresaId <= 0) return BadRequest("empresaId inválido.");
			if (req == null) return BadRequest("Payload inválido.");

			// Permissão pelo token: admin altera qualquer empresa; gerente só a própria.
			var tipo = User.GetUserType();
			var empresaJwt = User.GetEmpresaId();
			var isOwner = tipo == TypeUser.admin;
			var isGerenteDaPropria = tipo == TypeUser.gerente && empresaJwt == empresaId;
			if (!isOwner && !isGerenteDaPropria)
				return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para alterar esta empresa.");

			// Só o dono ativa/desativa empresa; gerente nunca altera status.
			if (!isOwner) req.Empresa.Status = null;

			var result = await _empresasService.UpdateEmpresa(req.Empresa, empresaId);
			if (result) return Ok(true);

			return BadRequest("Falha ao atualizar empresa.");
		}

		[HttpDelete]
		[Route("delete/{id}")]
		public async Task<IActionResult> DeleteEmpresa(int id)
		{
			try
			{
				bool result = await _empresasService.DeleteEmpresa(id);
				if (result)
					return Ok("Empresa excluída com sucesso.");
				else
					return BadRequest("Falha ao excluir empresa.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost]
		[Route("toggle-status/{id}")]
		public async Task<IActionResult> ToggleEmpresaStatus(int id)
		{
			try
			{
				bool result = await _empresasService.ToggleEmpresaStatus(id);
				if (result)
					return Ok("Status da empresa alterado com sucesso.");
				else
					return BadRequest("Falha ao alterar o status da empresa.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet]
		[Route("getEmpresasPaged")]
		public async Task<IActionResult> GetEmpresasPaged([FromQuery] FiltersDTO filtersDTO)
		{
			var result = await _empresasService.GetEmpresasPaged(filtersDTO);
			if (result != null)
				return Ok(result);
			else
				return BadRequest();
		}

		[HttpGet("getById/{empresaId}")]
		public async Task<IActionResult> GetById(int empresaId)
		{
			if (empresaId <= 0) return BadRequest("empresaId inválido.");

			var empresa = await _empresasService.GetEmpresaById(empresaId);
			if (empresa == null) return NotFound("Empresa não encontrada.");

			var dto = new EmpresasDTO
			{
				Id = empresa.Id,
				Name = empresa.Name,
				Status = empresa.Status,
				CNPJ = empresa.CNPJ,

				TradeName = empresa.TradeName,
				AppName = empresa.AppName,
				LogoBase64 = empresa.LogoBase64,
				LogoContentType = empresa.LogoContentType,

				ContactEmail = empresa.ContactEmail,
				Phone = empresa.Phone,
				Address = empresa.Address,
				PrimaryColor = empresa.PrimaryColor
			};

			return Ok(dto);
		}
	}
}
