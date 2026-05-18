using ControlApi.Middleware;
using Core.Mapping;
using Infrastructure.Authenticate;
using Infrastructure.ServiceExtension;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Kestrel + tamanhos de request (uploads de fotos/anexos do relatório).
// -----------------------------------------------------------------------------
builder.WebHost.ConfigureKestrel(serverOptions =>
{
	// 100 MB — fotos de campo, anexos de suporte etc.
	serverOptions.Limits.MaxRequestBodySize = 104857600;
	serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
	serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

builder.Services.Configure<FormOptions>(options =>
{
	options.ValueLengthLimit = int.MaxValue;
	options.MultipartBodyLengthLimit = 104857600;
	options.MemoryBufferThreshold = 10485760;
	options.MultipartBoundaryLengthLimit = int.MaxValue;
	options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.Configure<KestrelServerOptions>(options =>
{
	options.Limits.MaxRequestBodySize = 104857600;
	options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
});

// -----------------------------------------------------------------------------
// Logging
// -----------------------------------------------------------------------------
builder.Host.UseSerilog((context, loggerConfig) =>
{
	loggerConfig
			.ReadFrom.Configuration(context.Configuration)
			.Enrich.FromLogContext()
			.Enrich.WithEnvironmentName()
			.Enrich.WithMachineName()
			.Enrich.WithExceptionDetails()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// -----------------------------------------------------------------------------
// DI — services do domínio
// -----------------------------------------------------------------------------
builder.Services.AddDIServices(builder.Configuration);

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmpresasService, EmpresasService>();
builder.Services.AddScoped<IObrasService, ObrasService>();
builder.Services.AddScoped<IGrupoDeObrasService, GrupoDeObrasService>();
builder.Services.AddScoped<IModeloTextoService, ModeloTextoService>();
builder.Services.AddScoped<IModeloTextoVariavelService, ModeloTextoVariavelService>();
builder.Services.AddScoped<IMaoDeObraService, MaoDeObraService>();
builder.Services.AddScoped<IEquipamentosService, EquipamentosService>();
builder.Services.AddScoped<ITiposOcorrenciaService, TiposOcorrenciaService>();
builder.Services.AddScoped<IDespesasService, DespesasService>();
builder.Services.AddScoped<ISupportTicketsService, SupportTicketsService>();
builder.Services.AddScoped<IChecklistService, ChecklistService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();
builder.Services.AddScoped<IOcorrenciaService, OcorrenciaService>();
builder.Services.AddScoped<IObraChecklistService, ObraChecklistService>();
builder.Services.AddScoped<IChecklistItemService, ChecklistItemService>();
builder.Services.AddScoped<IAtividadeRecenteService, AtividadeRecenteService>();
builder.Services.AddScoped<IPlanoService, PlanoService>();
builder.Services.AddScoped<IAssinaturaService, AssinaturaService>();
builder.Services.AddSingleton<IJWTManager, JWTManager>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddAutoMapper(cfg => { }, typeof(EmpresasMappingProfile));

// -----------------------------------------------------------------------------
// Autenticação JWT
//
// Comportamento intencionalmente preservado: ValidateIssuer/ValidateAudience
// permanecem desligados para não invalidar tokens em circulação. Quando o
// operador da produção quiser endurecer, basta trocar para `true` aqui e
// preencher Jwt:Issuer / Jwt:Audience no appsettings — o JWTManager já está
// preparado para emitir essas claims quando configuradas.
// -----------------------------------------------------------------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer(options =>
		{
			var issuer = builder.Configuration["Jwt:Issuer"];
			var audience = builder.Configuration["Jwt:Audience"];
			var key = builder.Configuration["Jwt:Key"];

			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? "")),
				ClockSkew = TimeSpan.Zero,
			};
		});

// -----------------------------------------------------------------------------
// Controllers + JSON options. UMA chamada só (antes estavam duplicadas — a
// segunda sobrescrevia a primeira de forma silenciosa).
// -----------------------------------------------------------------------------
builder.Services.AddControllers()
		.AddJsonOptions(options =>
		{
			options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			options.JsonSerializerOptions.MaxDepth = 64;
		});

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	options.SerializerOptions.MaxDepth = 64;
});

builder.Services.AddEndpointsApiExplorer();

// -----------------------------------------------------------------------------
// Compressão de resposta (Gzip/Brotli)
// -----------------------------------------------------------------------------
builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
	options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
});

// -----------------------------------------------------------------------------
// Swagger
// -----------------------------------------------------------------------------
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new() { Title = "ConfereSET.Api", Version = "v1" });
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
	});
	c.AddSecurityRequirement(new OpenApiSecurityRequirement()
		{
				{
						new OpenApiSecurityScheme
						{
								Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
								Scheme = "oauth2",
								Name = "Bearer",
								In = ParameterLocation.Header,
						},
						new List<string>()
				}
		});
});

// -----------------------------------------------------------------------------
// CORS — comportamento intencionalmente mantido como antes para não quebrar
// previews Vercel de outras branches/projetos em uso hoje.
// -----------------------------------------------------------------------------
const string CorsPolicyName = "Frontend";
var allowedOrigins = new[]
{
		"https://confere-set-front.vercel.app",
		"http://localhost:3000",
		"http://127.0.0.1:3000",
		"http://localhost:5173",
		"http://127.0.0.1:5173",
};

builder.Services.AddCors(options =>
{
	options.AddPolicy(CorsPolicyName, p =>
			p.SetIsOriginAllowed(origin =>
			{
				if (string.IsNullOrWhiteSpace(origin)) return false;
				if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
				return origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase);
			})
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
	);
});

// -----------------------------------------------------------------------------
// ForwardedHeaders — comportamento intencionalmente mantido como antes para
// não quebrar o setup atual atrás do Nginx em produção.
// -----------------------------------------------------------------------------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

var app = builder.Build();

// Compressão antes de qualquer middleware que produza payload.
app.UseResponseCompression();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
app.UseForwardedHeaders();
app.UseSerilogRequestLogging(options =>
{
	options.GetLevel = (httpContext, elapsed, ex) =>
	{
		if (ex != null || httpContext.Response.StatusCode > 499) return LogEventLevel.Error;
		if (httpContext.Response.StatusCode > 399) return LogEventLevel.Warning;
		return LogEventLevel.Information;
	};
});

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(CorsPolicyName);
app.MigrateDatabase();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
