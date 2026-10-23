# Política de dependências (NuGet / .NET) — Confere SET Back

Pacotes legítimos às vezes são sequestrados e publicados com código malicioso; isso costuma ser
descoberto e retirado **em poucos dias**. Por isso: nada recém-publicado e nunca a última versão
automaticamente. (Mesma política do front — ver `DEPENDENCIAS.md` do ConfereSET-front.)

## Regras

1. **Só versões com pelo menos 7 dias de publicadas** (confira a data na aba *Versions* do pacote
   em nuget.org).
2. **Não usar a última versão.** Prefira **uma ou duas versões atrás** da mais recente (que também
   tenha os 7 dias).
3. **Versão exata** no `.csproj` (`Version="9.0.15"`), sem curingas (`*`) nem faixas.
4. **Correção de segurança tem prioridade:** use a **menor versão que corrige** e que já tenha 7
   dias. Se só existir correção numa versão mais nova que isso, combine com o responsável técnico e
   registre a decisão no PR.
5. **Pacote sem uso sai.** Pacote referenciado e não usado só traz risco (foi o caso de
   BouncyCastle, SharpCompress, ImageSharp, SkiaSharp, EPPlus, ClosedXML, Pdfium, iText… removidos
   em 23/10/2026).

## Auditoria automática

O `Directory.Build.props` liga a auditoria do NuGet para **todos os projetos**, incluindo pacotes
indiretos (`NuGetAuditMode=all`). Todo `dotnet restore`/`build` mostra avisos **NU1901–NU1904**
quando surgir vulnerabilidade — **não ignore esses avisos**.

```bash
# Lista pacotes vulneráveis (diretos e indiretos)
dotnet list ControlApi/ControlApi.csproj package --vulnerable --include-transitive

# Pacotes descontinuados e desatualizados
dotnet list ControlApi/ControlApi.csproj package --deprecated
dotnet list ControlApi/ControlApi.csproj package --outdated
```

## Como atualizar um pacote

1. Em nuget.org → pacote → *Versions*: escolha a versão (7+ dias, 1–2 atrás da última).
2. Troque a versão em **todos** os `.csproj` que referenciam o pacote (mesma versão em todos).
3. `dotnet build ControlApi/ControlApi.csproj` sem erros e sem avisos NU190x.
4. `dotnet list ... --vulnerable --include-transitive` limpo.

## Runtime .NET

- O Dockerfile usa `mcr.microsoft.com/dotnet/sdk:9.0` e `aspnet:9.0`: a cada deploy entra o
  **patch mais recente** do .NET 9 (as correções de segurança do runtime são a exceção à regra dos 7
  dias — aplique assim que sair).
- ⚠️ **O .NET 9 perde o suporte em 10/11/2026.** Depois disso não recebe mais correções de
  segurança. Migrar para o **.NET 10 (LTS, suporte até 14/11/2028)**: `TargetFramework` `net10.0`,
  pacotes `Microsoft.*`/EF Core/Npgsql na linha 10.x e imagens Docker `:10.0`.

## Rotina

- **Toda semana:** conferir os avisos NU190x do build/deploy. O Dependabot
  (`.github/dependabot.yml`) abre PRs com espera de 7 dias e avisa vulnerabilidades.
- **Todo mês:** `--outdated` e atualizar seguindo as regras.

## Última revisão — 23/10/2026

Antes: 2 pacotes diretos vulneráveis (BouncyCastle 1.8.9, SharpCompress 0.37.2) e 5 indiretos
(SixLabors.ImageSharp 3.1.4, SkiaSharp 2.80.3, System.Formats.Asn1 8.0.0, System.IO.Packaging 6.0.0,
System.Security.Cryptography.Pkcs 8.0.0) — 9 avisos altos. Nenhum era usado pelo código: os pacotes
que os traziam foram removidos. Resultado: **0 pacotes vulneráveis, 0 descontinuados**, build sem
erros.
