namespace Services
{
	/// <summary>
	/// Validação de negócio com lista de pendências (concluir checklist / enviar relatório).
	/// O controller devolve 400 com <c>{ message, pendencias }</c>.
	/// </summary>
	public class PendenciasException : Exception
	{
		public IReadOnlyList<object> Pendencias { get; }

		public PendenciasException(string message, IEnumerable<object> pendencias) : base(message)
		{
			Pendencias = pendencias.ToList();
		}
	}

	/// <summary>Regra de negócio que deve virar 403 (ex.: execução concluída).</summary>
	public class ForbiddenOperationException : Exception
	{
		public ForbiddenOperationException(string message) : base(message) { }
	}
}
