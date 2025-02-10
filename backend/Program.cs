using backend.BL.Converter;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsApi",
                    builder => builder.WithOrigins(new string[]{
                        "http://localhost:4200"
                    })
                 .SetIsOriginAllowed((host) => true)
                 .AllowAnyMethod()
                 .AllowAnyHeader());
            });

var app = builder.Build();

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
