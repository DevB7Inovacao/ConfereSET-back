using ControlApi.Middleware;
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

// Configurar Kestrel para aceitar arquivos grandes
builder.WebHost.ConfigureKestrel(serverOptions =>
{
	// Aumentar limite de tamanho do corpo da requisição para 100MB
	serverOptions.Limits.MaxRequestBodySize = 104857600; // 100 MB em bytes

	// Opcional: Aumentar timeout para uploads grandes
	serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
	serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
});

// Configurar limites do servidor HTTP
builder.Services.Configure<FormOptions>(options =>
{
	options.ValueLengthLimit = int.MaxValue;
	options.MultipartBodyLengthLimit = 104857600; // 100 MB
	options.MemoryBufferThreshold = 10485760; // 10 MB buffer threshold
	options.MultipartBoundaryLengthLimit = int.MaxValue;
	options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Configurar limites do Kestrel (alternativa via configuração)
builder.Services.Configure<KestrelServerOptions>(options =>
{
	options.Limits.MaxRequestBodySize = 104857600; // 100 MB
	options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
});

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

builder.Services.AddSingleton<IJWTManager, JWTManager>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configurar compressão de resposta para otimizar transferência
builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
	options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
});

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

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
	options.SerializerOptions.MaxDepth = 64;
});

builder.Services.AddControllers()
		.AddJsonOptions(options =>
		{
			options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			options.JsonSerializerOptions.MaxDepth = 64;
		});

var app = builder.Build();

// Aplicar compressão de resposta
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