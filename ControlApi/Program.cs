using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Services;
using System.Text;
using Infrastructure.ServiceExtension;
using Microsoft.OpenApi.Models;
using ControlApi.Middleware;
using Serilog;
using Serilog.Exceptions;
using Serilog.Events;
using Core.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDIServices(builder.Configuration);

//builder.Host.UseSerilog((context, loggerConfig) =>
//{
//	loggerConfig
//	.ReadFrom.Configuration(context.Configuration)
//	.Enrich.FromLogContext()
//	.Enrich.WithEnvironmentName()
//	.Enrich.WithMachineName()
//	.Enrich.WithExceptionDetails()
//    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error); 
//    //.WriteTo.Console();
//});

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        );
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmpresasService, EmpresasService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = builder.Configuration["Jwt:Issuer"],
				ValidAudience = builder.Configuration["Jwt:Audience"],
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
				ClockSkew = TimeSpan.Zero,
			};
		});
builder.Services.AddSingleton<IJWTManager, JWTManager>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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
								Reference = new OpenApiReference
								{
										Type = ReferenceType.SecurityScheme,
										Id = "Bearer"
								},
								Scheme = "oauth2",
								Name = "Bearer",
								In = ParameterLocation.Header,
						},
						new List<string>()
				}
		});
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.MigrateDatabase();
//app.UseHttpsRedirection();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null || httpContext.Response.StatusCode > 499)
        {

            return LogEventLevel.Error;
        }
        else if (httpContext.Response.StatusCode > 399)
        {
            return LogEventLevel.Warning;
        }
        else
        {
            return LogEventLevel.Information;
        }
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
