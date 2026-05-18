using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Health check público. Reporta status do app e do banco (CanConnect).
	/// <para>
	/// O namespace foi padronizado para <c>ControlApi.Controllers</c> (estava
	/// <c>API.Controllers</c>, divergindo dos demais). A reflexão para descobrir o
	/// <c>DbContext</c> foi removida em favor da injeção direta — bem mais robusta a
	/// renomes futuros.
	/// </para>
	/// </summary>
	[AllowAnonymous]
	[Route("api/[controller]")]
	[ApiController]
	public class HealthController : ControllerBase
	{
		private readonly DbContextClass _dbContext;
		private readonly ILogger<HealthController> _logger;

		public HealthController(DbContextClass dbContext, ILogger<HealthController> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		[HttpGet]
		[Route("")]
		public async Task<IActionResult> Get()
		{
			var ok = true;
			string db = "unknown";

			try
			{
				var can = await _dbContext.Database.CanConnectAsync();
				db = can ? "up" : "down";
				ok = ok && can;
			}
			catch (Exception ex)
			{
				// Antes o catch era vazio — qualquer falha de credencial/firewall ficava
				// invisível. Agora registramos a causa para troubleshooting.
				_logger.LogWarning(ex, "Health: CanConnectAsync falhou.");
				db = "down";
				ok = false;
			}

			return Ok(new
			{
				status = ok ? "ok" : "degraded",
				service = "ConfereSET API",
				time = DateTime.UtcNow,
				db
			});
		}
	}
}