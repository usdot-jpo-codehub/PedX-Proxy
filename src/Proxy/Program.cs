using AspNetCore.Authentication.ApiKey;
using Microsoft.OpenApi.Models;
using Proxy.Configs;
using Proxy.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load configs
var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var baseConfigPath = Path.Combine(programDataPath, "PED-X Proxy");
var altAppSettingsPath = Path.Combine(baseConfigPath, "appsettings.json");

if (Path.Exists(altAppSettingsPath)) 
    builder.Configuration.SetBasePath(baseConfigPath);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("security.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("intersections.json", optional: false, reloadOnChange: true);

builder.Services.Configure<ProxyConfig>(builder.Configuration);

// Setup logging
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

// Setup services
builder.Services.AddWindowsService();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IAdapterFactory, AdapterFactory>();

// Add authentication using API Key
builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(options =>
    {
        options.Realm = "Proxy API";
        options.KeyName = "X-API-KEY";
    });

builder.Services.AddAuthorization();

// Suppress the default map client errors for Swagger
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opt =>
    {
        opt.SuppressMapClientErrors = true;
    });

// Add API documentation with ApiKey authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "PED-X Proxy", Version = "v1" });
    opt.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKey",
        Description = "Authorization by API key needed to access the endpoints"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger/OpenAPI in development environment
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Require HTTPS in non-development environments
    app.UseHttpsRedirection();
}

app.MapHealthChecks("/healthz");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();