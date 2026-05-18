using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Leitura do histórico de atividades dos operadores. As atividades são produzidas por
    /// outros services do back (não há criação manual exposta aqui).
    /// <para>
    /// Regras:
    /// <list type="bullet">
    /// <item><b>byOperador</b>: qualquer usuário logado pode ler as próprias atividades.
    /// Admin/gerente da mesma empresa pode ler as atividades de qualquer operador da empresa.
    /// O operador alvo precisa pertencer à empresa do JWT.</item>
    /// <item><b>byEmpresa</b>: restrito a admin/gerente. <c>empresaId</c> é forçado pelo JWT,
    /// ignorando o valor da URL — a rota permanece, mas o filtro real é o do token.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AtividadeRecenteController : ControllerBase
    {
        private readonly IAtividadeRecenteService _service;
        private readonly IEmpresasService _empresasService;

        public AtividadeRecenteController(IAtividadeRecenteService service, IEmpresasService empresasService)
        {
            _service = service;
            _empresasService = empresasService;
        }

        [HttpGet("operador/{operadorId}")]
        public async Task<IActionResult> GetByOperador(int operadorId, [FromQuery] FiltersAtividadeRecenteDTO filters)
        {
            try
            {
                if (ope