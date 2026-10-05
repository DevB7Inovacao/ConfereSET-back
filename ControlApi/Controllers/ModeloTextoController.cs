using ControlApi;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ModeloTextoController : ControllerBase
    {
        private readonly IModeloTextoService _service;
        private readonly IRelatorioService _relatorioService;
        private readonly IS3Service _s3Service;

        /// <summary>Imagens por envio na importação de documentos (o front manda em lotes).</summary>
        private const int MaxImagensPorEnvio = 20;

        public ModeloTextoController(IModeloTextoService service, IRelatorioService relatorioService, IS3Service s3Service)
        {
            _service = service;
            _relatorioService = relatorioService;
            _s3Service = s3Service;
        }

        public class UploadImagensModeloRequest
        {
            public List<UploadImagemModeloItem>? Imagens { get; set; }
        }

        public class UploadImagemModeloItem
        {
            public string? ImagemBase64 { get; set; }
            public string? NomeArquivo { get; set; }
        }

        /// <summary>
        /// Envia as imagens de um documento importado (Word) para o S3 e devolve as URLs, na mesma
        /// ordem. Assim o modelo guarda só o link (antes a imagem ia embutida em base64 no HTML e
        /// era copiada para cada relatório). Tudo ou nada: se uma falhar, as já enviadas são apagadas.
        /// </summary>
        [HttpPost("imagens")]
        [RequestSizeLimit(150_000_000)]
        public async Task<IActionResult> UploadImagens([FromBody] UploadImagensModeloRequest req)
        {
            var semPermissao = ChecarPermissaoEscrita();
            if (semPermissao != null) return semPermissao;

            var imagens = req?.Imagens ?? new List<UploadImagemModeloItem>();
            if (imagens.Count == 0) return BadRequest("Nenhuma imagem enviada.");
            if (imagens.Count > MaxImagensPorEnvio) return BadRequest($"Envie no máximo {MaxImagensPorEnvio} imagens por vez.");

            List<ImagemValidada> validadas;
            try
            {
                validadas = imagens.Select(i => ImageValidation.Validar(i.ImagemBase64, i.NomeArquivo, "imagem")).ToList();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var urls = new List<string>();
            try
            {
                foreach (var img in validadas)
                    urls.Add(await _s3Service.UploadImageAsync(img.Bytes, $"modelo_{User.GetEmpresaId()}_{img.NomeArquivo}", img.ContentType));
                return Ok(new { urls });
            }
            catch (Exception ex)
            {
                foreach (var url in urls) await _s3Service.DeleteImageAsync(url);
                return BadRequest($"Falha ao enviar as imagens: {ex.Message}");
            }
        }

        /// <summary>
        /// Converte um .doc (Word 97-2003) em .docx para a importação de modelos. O front segue com
        /// o .docx devolvido pelo mesmo caminho do .docx enviado direto. Nada é gravado aqui.
        /// </summary>
        [HttpPost("converter-doc")]
        [RequestSizeLimit(WordLegadoConverter.TamanhoMaximo + 1_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = WordLegadoConverter.TamanhoMaximo + 1_000_000)]
        public async Task<IActionResult> ConverterDoc(IFormFile? arquivo)
        {
            var semPermissao = ChecarPermissaoEscrita();
            if (semPermissao != null) return semPermissao;
            if (arquivo == null || arquivo.Length == 0) return BadRequest("Envie o arquivo .doc.");
            if (arquivo.Length > WordLegadoConverter.TamanhoMaximo)
                return BadRequest("O arquivo passa de 30 MB. Reduza as imagens no Word e tente de novo.");

            byte[] doc;
            using (var ms = new MemoryStream())
            {
                await arquivo.CopyToAsync(ms);
                doc = ms.ToArray();
            }

            try
            {
                // Conversão é CPU pura (sem I/O): fora da thread do request.
                var docx = await Task.Run(() => WordLegadoConverter.ConverterParaDocx(doc));
                var nome = Path.GetFileNameWithoutExtension(arquivo.FileName ?? "documento") + ".docx";
                return Ok(new { nomeArquivo = nome, docxBase64 = Convert.ToBase64String(docx) });
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsAdminOrGerente() ? null : StatusCode(StatusCodes.Status403Forbidden, "Apenas administradores da empresa podem alterar cadastros.");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModeloTextoRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                req.EmpresaId = User.GetEmpresaId();
                var created = await _service.Create(req);
                return Ok(created.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersModeloTextoDTO filters)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filters.EmpresaId = User.GetEmpresaId();
            try
            {
                var result = await _service.GetPaged(filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var model = await _service.GetById(id);
                if (model == null) return NotFound("Modelo não encontrado.");
            if (model.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateModeloTextoRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.Update(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.Delete(id);
                return ok ? Ok(true) : BadRequest("Falha ao excluir.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                // FK Restrict (relatórios) ou vínculos com obras.
                return BadRequest("Modelo em uso por relatórios/obras; desative-o em vez de excluir.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.ToggleStatus(id);
                return ok ? Ok(true) : BadRequest("Falha ao alternar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}