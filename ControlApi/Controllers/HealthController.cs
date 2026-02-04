using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Repositories;

namespace API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public HealthController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var ok = true;
                string db = "unknown";

                try
                {
                    if (_unitOfWork is not null)
                    {
                        var ctxProp = _unitOfWork.GetType().GetProperty("DbContext")
                                     ?? _unitOfWork.GetType().GetProperty("_dbContext")
                                     ?? _unitOfWork.GetType().GetProperty("Context")
                                     ?? _unitOfWork.GetType().GetProperty("dbContext");

                        if (ctxProp != null)
                        {
                            var ctx = ctxProp.GetValue(_unitOfWork) as DbContext;
                            if (ctx != null)
                            {
                                var can = await ctx.Database.CanConnectAsync();
                                db = can ? "up" : "down";
                                ok = ok && can;
                            }
                            else
                            {
                                db = "skipped";
                            }
                        }
                        else
                        {
                            db = "skipped";
                        }
                    }
                }
                catch
                {
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "down",
                    service = "ConfereSET API",
                    time = DateTime.UtcNow,
                    error = ex.Message
                });
            }
        }
    }
}