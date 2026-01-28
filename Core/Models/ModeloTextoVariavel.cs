using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ModeloTextoVariavel : BaseModel
    {
        public required int EmpresaId { get; set; }

        /// <summary>
        /// Token: {{NOME_CLIENTE}}
        /// </summary>
        public required string Nome { get; set; }

        /// <summary>
        /// Nome amigável pra UI: "Nome do Cliente"
        /// </summary>
        public required string NomeAmigavel { get; set; }

        /// <summary>
        /// Caminho/propriedade que será resolvida depois:
        /// Ex: "Cliente.Nome" ou "Obra.Name"
        /// </summary>
        public required string NomePropriedade { get; set; }

        /// <summary>
        /// Categoria (para agrupar no front)
        /// </summary>
        public int Categoria { get; set; }

        /// <summary>
        /// Classe/escopo (opcional): "Cliente", "Obra", "Usuario"...
        /// </summary>
        public string? Classe { get; set; }

        /// <summary>
        /// Valor default (opcional) — útil pra variáveis tipo módulo/bloco.
        /// </summary>
        public string? Valor { get; set; }

        /// <summary>
        /// 1 = ativo, 0 = inativo
        /// </summary>
        public int Status { get; set; } = 1;
    }
}
