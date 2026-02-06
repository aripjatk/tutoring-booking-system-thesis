using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TutorApp.API.Data;
using TutorApp.API.Interfaces;
using TutorApp.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(opt =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TutorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<AccountCleanupService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var tokenKey = builder.Configuration["TokenKey"];
    if (string.IsNullOrEmpty(tokenKey)) {
        throw new Exception("CRITICAL ERROR: 'TokenKey' is missing from appsettings.json. application cannot start.");
    }
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors(options => {
    options.AddPolicy("TbsCors", p => p
    .WithOrigins(builder.Configuration["AppUrl"])
    .AllowAnyHeader()
    .AllowAnyMethod());
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("TbsCors");

app.UseWhen(context => context.GetEndpoint() == null, appBuilder =>
{
    var fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
    var contentTypeProvider = new FileExtensionContentTypeProvider();
    contentTypeProvider.Mappings.Clear();
    contentTypeProvider.Mappings[".html"] = "text/html";
    contentTypeProvider.Mappings[".css"] = "text/css";
    contentTypeProvider.Mappings[".js"] = "application/javascript";

    appBuilder.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        DefaultFileNames = new List<string> { "index.html" }
    });

    appBuilder.Use(async (context, next) =>
    {
        var path = context.Request.Path.Value?.TrimStart('/');
        if (string.IsNullOrEmpty(path))
        {
            await next();
            return;
        }

        if (path.Contains(".."))
        {
            context.Response.StatusCode = 404;
            return;
        }

        var parts = path.Split('/');
        bool isAllowed = false;
        if (parts.Length == 1) isAllowed = true;
        else if (parts[0].Equals("app", StringComparison.OrdinalIgnoreCase)) isAllowed = true;
        else if (parts[0].Equals("assets", StringComparison.OrdinalIgnoreCase)) isAllowed = true;

        if (!isAllowed)
        {
            context.Response.StatusCode = 404;
            return;
        }

        await next();
    });

    appBuilder.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        ContentTypeProvider = contentTypeProvider
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
