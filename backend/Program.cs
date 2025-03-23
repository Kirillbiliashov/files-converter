using System.Text;
using backend.BL.Converter;
using backend.BL.Integrations;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(sp => 
    new MongoClient(builder.Configuration.GetValue<string>("MongoDbSettings:ConnectionString")));
builder.Services.AddScoped<IMongoDatabase>(sp => 
    sp.GetRequiredService<IMongoClient>().GetDatabase(builder.Configuration.GetValue<string>("MongoDbSettings:DatabaseName")));

builder.Services.AddSingleton<GoogleSignInManager>();
builder.Services.AddSingleton<DropboxSignInManager>();

builder.Services.AddSingleton<AzureBlobService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddGoogle(options =>
{
    IConfigurationSection googleAuthSection = builder.Configuration.GetSection("Authentication:Google");
    options.ClientId = googleAuthSection["ClientId"];
    options.ClientSecret = googleAuthSection["ClientSecret"];
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddScoped<ImageMagickFileConverter>();
builder.Services.AddScoped<LibreOfficeFileConverter>();
builder.Services.AddScoped<Func<string, IFileConverter>>(provider => format =>
{
    var imageFormats = new HashSet<string> { "png", "jpg", "jpeg", "gif", "bmp", "tiff" };

    return imageFormats.Contains(format.ToLower())
        ? provider.GetRequiredService<ImageMagickFileConverter>()
        : provider.GetRequiredService<LibreOfficeFileConverter>();
});

builder.Services.AddScoped<GoogleSignInManager>();
builder.Services.AddScoped<DropboxSignInManager>();
builder.Services.AddScoped<Func<string, OAuthSignInManager>>(provider => integration =>
{
    if (integration == "Google")
    {
        return provider.GetRequiredService<GoogleSignInManager>();
    }
    if (integration == "Dropbox")
    {
        return provider.GetRequiredService<DropboxSignInManager>();
    }

    return null;
});

builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsApi",
                    builder => builder.WithOrigins(new string[]{
                        "http://localhost:4200"
                    })
                 .SetIsOriginAllowed((host) => true)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition"));
            });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsApi");
app.MapControllers();

app.Run();
