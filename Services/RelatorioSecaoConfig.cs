using Core.Enums;
using Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Services
{
	/// <summary>
	/// [v2] Leitura/normalização do JSON de configuração das seções configuráveis do relatório
	/// (atributo <c>data-config</c> do modelo, gravado em <c>RelatorioSecao.ConteudoJson</c>).
	/// Tolerante: JSON inválido/grande demais vira config vazia — nunca lança na criação do relatório.
	/// </summary>
	public static class RelatorioSecaoConfig
	{
		public const int MaxConfigChars = 20 * 1024;
		public const int MaxCamposFormulario = 50;
		public const int MaxPeriodosClima = 10;
		public const int MaxAssinantes = 10;
		private const int MaxTextoItem = 200;

		public static readonly string[] PeriodosPadrao = ["Manhã", "Tarde"];
		public static readonly string[] AssinantesPadrao = ["Responsável pela obra"];

		private static readonly Regex CampoSlugRegex = new("^[a-z0-9_-]{1,60}$", RegexOptions.Compiled);
		private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

		/// <summary>Tipos cuja config fica em ConteudoJson e que podem aparecer várias vezes (via data-campo).</summary>
		public static bool IsConfiguravel(TipoSecao tipo) =>
			tipo == TipoSecao.TextoLivre || tipo == TipoSecao.Fotos || tipo == TipoSecao.Clima
			|| tipo == TipoSecao.Assinatura || tipo == TipoSecao.Formulario || tipo == TipoSecao.Checklist
			|| tipo == TipoSecao.Despesas;

		/// <summary>Normaliza o <c>data-campo</c> (a-z0-9-_). Retorna <c>null</c> se ausente ou inválido.</summary>
		public static string? NormalizarCampo(string? campo)
		{
			var c = (campo ?? string.Empty).Trim().ToLowerInvariant();
			return CampoSlugRegex.IsMatch(c) ? c : null;
		}

		/// <summary>Parse tolerante: devolve o objeto JSON ou <c>null</c> (inválido, vazio, não-objeto ou &gt; 20 KB).</summary>
		public static JsonObject? Parse(string? raw)
		{
			if (string.IsNullOrWhiteSpace(raw) || raw.Length > MaxConfigChars) return null;
			try
			{
				return JsonNode.Parse(raw) as JsonObject;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Normaliza a config (aplica os limites de campos/períodos/assinantes) e devolve o JSON a gravar,
		/// ou <c>null</c> quando vazia/inválida.
		/// </summary>
		public static string? Normalizar(TipoSecao tipo, string? raw)
		{
			var obj = Parse(raw);
			if (obj == null) return null;

			switch (tipo)
			{
				case TipoSecao.Formulario:
					TruncarArray(obj, "campos", MaxCamposFormulario);
					break;
				case TipoSecao.Clima:
					TruncarArray(obj, "periodos", MaxPeriodosClima);
					break;
				case TipoSecao.Assinatura:
					TruncarArray(obj, "assinantes", MaxAssinantes);
					break;
			}

			var json = obj.ToJsonString(JsonOptions);
			return json.Length > MaxConfigChars ? null : json;
		}

		/// <summary>True se o texto de config (vindo de API) excede o limite aceito.</summary>
		public static bool ExcedeLimite(string? raw) => raw != null && raw.Length > MaxConfigChars;

		private static void TruncarArray(JsonObject obj, string chave, int max)
		{
			var key = obj.Select(kv => kv.Key).FirstOrDefault(k => string.Equals(k, chave, StringComparison.OrdinalIgnoreCase));
			if (key == null || obj[key] is not JsonArray arr) return;
			while (arr.Count > max) arr.RemoveAt(arr.Count - 1);
		}

		private static JsonNode? Get(JsonObject? obj, string chave)
		{
			if (obj == null) return null;
			if (obj.TryGetPropertyValue(chave, out var v)) return v;
			foreach (var kv in obj)
				if (string.Equals(kv.Key, chave, StringComparison.OrdinalIgnoreCase)) return kv.Value;
			return null;
		}

		private static string? GetString(JsonObject? obj, string chave)
		{
			var v = Get(obj, chave);
			if (v is JsonValue jv)
			{
				if (jv.TryGetValue<string>(out var s)) return s;
				return jv.ToJsonString();
			}
			return null;
		}

		public static bool GetBool(JsonObject? obj, string chave)
		{
			var v = Get(obj, chave);
			if (v is not JsonValue jv) return false;
			if (jv.TryGetValue<bool>(out var b)) return b;
			if (jv.TryGetValue<string>(out var s)) return string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1";
			if (jv.TryGetValue<int>(out var i)) return i != 0;
			return false;
		}

		public static int? GetInt(JsonObject? obj, string chave)
		{
			var v = Get(obj, chave);
			if (v is not JsonValue jv) return null;
			if (jv.TryGetValue<int>(out var i)) return i;
			if (jv.TryGetValue<double>(out var d) && d >= int.MinValue && d <= int.MaxValue) return (int)d;
			if (jv.TryGetValue<string>(out var s) && int.TryParse(s, out var p)) return p;
			return null;
		}

		private static List<string> GetStringArray(JsonObject? obj, string chave, int max)
		{
			var result = new List<string>();
			if (Get(obj, chave) is not JsonArray arr) return result;
			foreach (var n in arr)
			{
				string? s = null;
				if (n is JsonValue jv) s = jv.TryGetValue<string>(out var str) ? str : jv.ToJsonString();
				s = s?.Trim();
				if (string.IsNullOrWhiteSpace(s)) continue;
				if (s.Length > MaxTextoItem) s = s[..MaxTextoItem];
				result.Add(s);
				if (result.Count >= max) break;
			}
			return result;
		}

		public static bool Obrigatorio(JsonObject? cfg) => GetBool(cfg, "obrigatorio");

		public static List<string> Periodos(JsonObject? cfg)
		{
			var p = GetStringArray(cfg, "periodos", MaxPeriodosClima);
			return p.Count > 0 ? p : PeriodosPadrao.ToList();
		}

		public static List<string> Assinantes(JsonObject? cfg)
		{
			var a = GetStringArray(cfg, "assinantes", MaxAssinantes);
			return a.Count > 0 ? a : AssinantesPadrao.ToList();
		}

		public static int? MinFotos(JsonObject? cfg) => GetInt(cfg, "minFotos");

		public static int? ChecklistId(JsonObject? cfg) => GetInt(cfg, "checklistId");

		public sealed record CampoFormulario(string Key, string Label, string Tipo, bool Obrigatorio);

		/// <summary>Campos do formulário (somente os que têm label), limitados a 50.</summary>
		public static List<CampoFormulario> Campos(JsonObject? cfg)
		{
			var result = new List<CampoFormulario>();
			if (Get(cfg, "campos") is not JsonArray arr) return result;
			var i = 0;
			foreach (var n in arr)
			{
				i++;
				if (n is not JsonObject c) continue;
				var label = GetString(c, "label")?.Trim();
				if (string.IsNullOrWhiteSpace(label)) continue;
				if (label.Length > MaxTextoItem) label = label[..MaxTextoItem];
				var key = GetString(c, "key")?.Trim();
				result.Add(new CampoFormulario(
					string.IsNullOrWhiteSpace(key) ? $"campo-{i}" : key,
					label,
					GetString(c, "tipo")?.Trim() ?? "texto",
					GetBool(c, "obrigatorio")));
				if (result.Count >= MaxCamposFormulario) break;
			}
			return result;
		}

		/// <summary>Nomes (RelatorioSecaoItem.Nome) dos itens que a seção deve ter conforme a config.</summary>
		public static List<string?> NomesItensPadrao(TipoSecao tipo, JsonObject? cfg) => tipo switch
		{
			TipoSecao.TextoLivre => [null],
			TipoSecao.Fotos => ["Fotos"],
			TipoSecao.Clima => Periodos(cfg).Cast<string?>().ToList(),
			TipoSecao.Assinatura => Assinantes(cfg).Cast<string?>().ToList(),
			TipoSecao.Formulario => Campos(cfg).Select(c => (string?)c.Label).ToList(),
			_ => new List<string?>()
		};

		/// <summary>Itens iniciais da seção (sem RelatorioSecaoId — preencher ou usar via navegação).</summary>
		public static List<RelatorioSecaoItem> ItensPadrao(TipoSecao tipo, JsonObject? cfg) =>
			NomesItensPadrao(tipo, cfg).Select(n => new RelatorioSecaoItem { Nome = n, Descricao = null }).ToList();

		/// <summary>Título padrão de exibição por tipo (usado nas pendências quando a seção não tem título).</summary>
		public static string TituloPadrao(TipoSecao tipo) => tipo switch
		{
			TipoSecao.Local => "Local",
			TipoSecao.MaoDeObra => "Mão de obra",
			TipoSecao.Equipamentos => "Equipamentos",
			TipoSecao.TextoLivre => "Texto livre",
			TipoSecao.Fotos => "Fotos",
			TipoSecao.Comentarios => "Comentários",
			TipoSecao.Ocorrencias => "Ocorrências",
			TipoSecao.Clima => "Condições do tempo",
			TipoSecao.Assinatura => "Assinaturas",
			TipoSecao.Formulario => "Formulário",
			TipoSecao.Checklist => "Conferelist",
			TipoSecao.Despesas => "Despesas",
			_ => "Seção"
		};

		/// <summary>Texto "vazio" ignorando tags HTML simples e &amp;nbsp;.</summary>
		public static bool TextoVazio(string? texto)
		{
			if (string.IsNullOrWhiteSpace(texto)) return true;
			var semTags = Regex.Replace(texto, "<[^>]*>", " ");
			semTags = semTags.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
			return string.IsNullOrWhiteSpace(semTags);
		}

		/// <summary>Lê uma propriedade string de um JSON de valor (ex.: Descricao da seção Clima).</summary>
		public static string? LerCampoValor(string? json, string chave)
		{
			var obj = Parse(json);
			return GetString(obj, chave);
		}
	}
}
