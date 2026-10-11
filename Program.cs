using Microsoft.EntityFrameworkCore;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;
using Signal_CRUD_Demo_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databasePath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
    options.UseSqlite($"Data Source={databasePath}");
});
builder.Services.AddScoped<IPostService, PostService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    if (!await dbContext.Posts.AnyAsync())
    {
        dbContext.Posts.AddRange(Enumerable.Range(1, 100).Select(id => new PostEntity
        {
            Title = $"Post {id}",
            Body = $"This is the body of post {id}."
        }));
        await dbContext.SaveChangesAsync();
    }
}

app.Run();

public partial class Program;
