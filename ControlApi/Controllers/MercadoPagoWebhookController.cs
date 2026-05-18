using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Route("api/webhook/mercadopago")]
    [ApiController]
    public class MercadoPagoWebhookController : ControllerBase
    {
        private readonly IAssinaturaService _assinaturaService;

        public MercadoPagoWebhookController(IAssinaturaService assinaturaService)
        {
            _assinaturaService = assinaturaService;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] MercadoPagoWebhookPayload payload)
        {
            if (payload?.Data?.Id == null) return Ok();

            try
            {
                if (payload.Type == "subscription_preapproval")
                    await _assinaturaService.ProcessarWebhookAssinatura(payload.Data.Id);

                if (payload.Type == "payment")
                    await _assinaturaService.ProcessarWebhookPagamento(payload.Data.Id);
            }
            catch
            {
                return Ok();
            }

            return Ok();
        }
    }
}