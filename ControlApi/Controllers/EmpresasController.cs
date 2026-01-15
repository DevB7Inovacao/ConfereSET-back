using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using SharpCompress;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        IEmpresasService _empresasService;

        public EmpresasController(IJWTManager jWTManager, IEmpresasService empresasService)
        {
            this._jWTManager = jWTManager;
            this._empresasService = empresasService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateEmpresa([FromBody] EmpresasDTO empresas) 
        {
            try
            {
                var empresa = new Empresas()
                {
                    Name = empresas.Name,
                    Status = empresas.Status ?? true,
                    CNPJ = empresas.CNPJ ?? "",
                };

                var result = await _empresasService.CreateEmpresa(empresa);

                if (result.Id > 0)
                    return Ok("Empresa cadastrada com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar empresa.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> UpdateEmpresa(EmpresasDTO empresas, int idEmpresa)
        {
            if (empresas != null)
            {
                var result = await _empresasService.UpdateEmpresa(empresas, idEmpresa);
                if (result)
                    return Ok(result);
                else
                    return BadRequest();
            }
            else
            {
                return BadRequest();
            }
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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

        [AllowAnonymous]
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
    }
}
