# Instruções para o projeto Confere SET (back)

## Dependências — obrigatório

Siga `DEPENDENCIAS.md`. Em resumo:

- Só use versões de pacotes NuGet publicadas há **pelo menos 7 dias**.
- **Não use a última versão**: prefira uma ou duas versões atrás da mais recente.
- Versão **exata** no `.csproj`; mesma versão em todos os projetos.
- Correção de segurança: menor versão corrigida que tenha 7+ dias.
- Não adicione pacote sem uso; remova os que deixarem de ser usados.
- Antes de concluir: `dotnet build ControlApi/ControlApi.csproj` sem erros e sem avisos NU190x, e
  `dotnet list ControlApi/ControlApi.csproj package --vulnerable --include-transitive` limpo.
