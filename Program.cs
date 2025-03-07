using ApiContabsv.Models.Contabilidad;
using ApiContabsv.Models.Contabsv;
using ApiContabsv.Models.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configurar la conexi�n a la base de datos
var connectionString = builder.Configuration.GetConnectionString("SeguridadConnection");
builder.Services.AddDbContext<SeguridadContext>(options =>
    options.UseSqlServer(connectionString));

var connectionStringContabsv = builder.Configuration.GetConnectionString("ContabsvConnection");
builder.Services.AddDbContext<ContabsvContext>(options =>
    options.UseSqlServer(connectionStringContabsv));
    
var connectionStringContabilidad = builder.Configuration.GetConnectionString("ContabilidadConnection");
builder.Services.AddDbContext<ContabilidadContext>(options =>
    options.UseSqlServer(connectionStringContabilidad));


// Configurar JSON para ignorar referencias circulares
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()    // Permite cualquier origen
                   .AllowAnyMethod()    // Permite cualquier método (GET, POST, etc.)
                   .AllowAnyHeader();   // Permite cualquier encabezado
        });
});



// Configurar Swagger con seguridad y documentación XML
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

    // Intentar cargar la documentación XML (evitar error si no existe)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});


var app = builder.Build();

// Middleware de enrutamiento (NECESARIO)
app.UseRouting();

// Habilitar Swagger en todos los entornos
app.UseSwagger();
app.UseSwaggerUI();

// Aplicar política de CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseAuthorization();

// Middleware de autenticación con API Key (DEBE IR DESPUÉS de UseAuthorization)
app.Use(async (context, next) =>
{
    var config = context.RequestServices.GetRequiredService<IConfiguration>();
    var apiKey = config.GetValue<string>("AuthSettings:ApiKey");

    if (!context.Request.Headers.TryGetValue("X-AUTH-TOKEN", out var providedKey) || providedKey != apiKey)
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("Unauthorized: Invalid API Key");
        return;
    }

    await next();
});

// Mapear controladores después de configurar middleware
app.MapControllers();

app.Run();