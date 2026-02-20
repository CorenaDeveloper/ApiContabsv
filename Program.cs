using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Contabsv;
using ApiContabsv.Models.Cultivo;
using ApiContabsv.Models.Dte;
using ApiContabsv.Models.Seguridad;
using ApiContabsv.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

// ============================================
// CONFIGURACIÓN INICIAL DE SERILOG
// ============================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("🚀 Iniciando aplicación ApiContabsv");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================
    // CONFIGURAR SERILOG COMO PROVIDER DE LOGGING
    // ============================================
    builder.Host.UseSerilog();

    // ============================================
    // CONFIGURACIÓN DE CONEXIONES A BASES DE DATOS
    // ============================================
    var connectionString = builder.Configuration.GetConnectionString("SeguridadConnection");
    builder.Services.AddDbContext<SeguridadContext>(options =>
        options.UseSqlServer(connectionString));

    var connectionStringContabsv = builder.Configuration.GetConnectionString("ContabsvConnection");
    builder.Services.AddDbContext<ContabsvContext>(options =>
        options.UseSqlServer(connectionStringContabsv));

    var connectionStringContabilidad = builder.Configuration.GetConnectionString("ContabilidadConnection");
    builder.Services.AddDbContext<ContabilidadContext>(options =>
        options.UseSqlServer(connectionStringContabilidad));

    var connectionStringCultivo = builder.Configuration.GetConnectionString("DteConnectionCultivo");
    builder.Services.AddDbContext<CultivoContext>(options =>
        options.UseSqlServer(connectionStringCultivo));

    var connectionStringDte = builder.Configuration.GetConnectionString("DteConnection");
    builder.Services.AddDbContext<dteContext>(options =>
        options.UseSqlServer(connectionStringDte));

    builder.Services.AddScoped<IHaciendaService, HaciendaService>();
    builder.Services.AddScoped<IDTEDocumentService, DTEDocumentService>();
    builder.Services.AddSingleton<PayPalService>();
    builder.Services.AddSingleton<WompiService>();
    // ============================================
    // CONFIGURACIÓN DE SERVICIOS BÁSICOS
    // ============================================
    builder.Services.AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddHttpClient();

    // ============================================
    // CONFIGURACIÓN DE SWAGGER CON GRUPOS
    // ============================================
    builder.Services.AddSwaggerGen(options =>
    {
        options.EnableAnnotations();

        // Configuración de seguridad con API Key
        options.AddSecurityDefinition("X-AUTH-TOKEN", new OpenApiSecurityScheme
        {
            Name = "X-AUTH-TOKEN",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKey",
            In = ParameterLocation.Header,
            Description = "Clave de API para acceder a las rutas protegidas. Ingresa el token en este campo."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "X-AUTH-TOKEN"
                    }
                },
                Array.Empty<string>()
            }
        });

        // ⭐ GRUPO 1: CONTABILIDAD
        options.SwaggerDoc("contabilidad", new OpenApiInfo
        {
            Version = "1.0",
            Title = "CONTABILIDAD APIs",
            Description = "APIs para el módulo de Contabilidad - Gestión de transacciones, cuentas contables, etc."
        });

        // ⭐ GRUPO 2: CONTABSV 
        options.SwaggerDoc("contabsv", new OpenApiInfo
        {
            Version = "1.0",
            Title = "CONTABSV APIs",
            Description = "APIs para el sistema ContabSV - Gestión de clientes, productos, facturas, etc."
        });

        // ⭐ GRUPO 3: SEGURIDAD
        options.SwaggerDoc("seguridad", new OpenApiInfo
        {
            Version = "1.0",
            Title = "SEGURIDAD APIs",
            Description = "APIs para el módulo de Seguridad - Usuarios, permisos, autenticación, etc."
        });

        // ⭐ GRUPO 4: REPORTES
        options.SwaggerDoc("reportes", new OpenApiInfo
        {
            Version = "1.0",
            Title = "REPORTES APIs",
            Description = "APIs para generación de reportes y consultas especializadas"
        });

        // ⭐ GRUPO 5: dte
        options.SwaggerDoc("dte", new OpenApiInfo
        {
            Version = "1.0",
            Title = "FACTURA ELECTRONICA APIs",
            Description = "APIs para generación facturación electronica."
        });

        // ⭐ GRUPO 6: CULTIVO EN CASA
        options.SwaggerDoc("cultivo", new OpenApiInfo
        {
            Version = "1.0",
            Title = "Cultivo APIs",
            Description = "APIs para monitoreo de cultivo por medio de placa arduino"
        });

        // Filtro para asignar controladores a grupos según su prefijo
        options.DocInclusionPredicate((docName, apiDesc) =>
        {
            if (apiDesc.RelativePath == null) return false;

            var controllerName = apiDesc.ActionDescriptor.RouteValues["controller"]?.ToLower() ?? "";

            return docName.ToLower() switch
            {
                "contabilidad" => controllerName.StartsWith("dbcontabilidad_"),
                "contabsv" => controllerName.StartsWith("dbcontabsv_"),
                "seguridad" => controllerName.StartsWith("dbseguridad_"),
                "reportes" => controllerName.StartsWith("reportes_"),
                "dte" => controllerName.StartsWith("dbdte_"),
                "cultivo" => controllerName.StartsWith("dbcultivo_"),
                _ => false
            };
        });

        // Documentación XML (si existe)
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // ============================================
    // USAR SERILOG PARA LOGGING DE REQUESTS HTTP
    // ============================================
    app.UseSerilogRequestLogging();

    // ============================================
    // MIDDLEWARE PIPELINE
    // ============================================
    app.UseRouting();

    // ============================================
    // CONFIGURACIÓN DE SWAGGER UI CON GRUPOS
    // ============================================
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Grupo Contabilidad
        c.SwaggerEndpoint("/swagger/contabilidad/swagger.json", "📊 CONTABILIDAD APIs");

        // Grupo ContabSV
        c.SwaggerEndpoint("/swagger/contabsv/swagger.json", "💼 CONTABSV APIs");

        // Grupo Seguridad  
        c.SwaggerEndpoint("/swagger/seguridad/swagger.json", "🔒 SEGURIDAD APIs");

        // Grupo Reportes
        c.SwaggerEndpoint("/swagger/reportes/swagger.json", "📈 REPORTES APIs");

        // Grupo DTE
        c.SwaggerEndpoint("/swagger/dte/swagger.json", "💼 DTE APIs");

        // Grupo Cultivo
        c.SwaggerEndpoint("/swagger/cultivo/swagger.json", " Cultivo APIs");

        c.RoutePrefix = string.Empty;
        c.DocumentTitle = "ContabSV API Documentation";
        c.DefaultModelsExpandDepth(-1); // Colapsar modelos por defecto
    });

    // ============================================
    // OTROS MIDDLEWARES
    // ============================================
    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseAuthorization();

    // Middleware de autenticación con API Key
    app.Use(async (context, next) =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = config.GetValue<string>("AuthSettings:ApiKey");

        if (!context.Request.Headers.TryGetValue("X-AUTH-TOKEN", out var providedKey) || providedKey != apiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized: Invalid API Key");
            return;
        }

        await next();
    });

    app.MapControllers();

    Log.Information("✅ Aplicación configurada correctamente");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ La aplicación falló al iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}