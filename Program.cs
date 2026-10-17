using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;
using Signal_CRUD_Demo_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SignalCrudDemo.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.MaxAge = TimeSpan.FromDays(7);
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databasePath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
    options.UseSqlite($"Data Source={databasePath}");
});
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Frontend");
app.UseAuthentication();

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
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
            "Email" TEXT NOT NULL,
            "PasswordHash" TEXT NOT NULL,
            "DateOfBirth" TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
        """);

    if (!await dbContext.Posts.AnyAsync())
    {
        dbContext.Posts.AddRange(Enumerable.Range(1, 100).Select(id => new PostEntity
        {
            Title = $"Post {id}",
            Body = $"This is the body of post {id}."
        }));
        await dbContext.SaveChangesAsync();
    }

    if (app.Environment.IsDevelopment() && !await dbContext.Users.AnyAsync())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();
        var testUsers = new[]
        {
            new UserEntity
            {
                Email = "alice@example.com",
                PasswordHash = string.Empty,
                DateOfBirth = new DateOnly(1995, 4, 12)
            },
            new UserEntity
            {
                Email = "bob@example.com",
                PasswordHash = string.Empty,
                DateOfBirth = new DateOnly(1990, 11, 3)
            }
        };
        var testPasswords = new[] { "PosthubTest123!", "ReaderTest123!" };

        for (var index = 0; index < testUsers.Length; index++)
        {
            testUsers[index].PasswordHash = hasher.HashPassword(testUsers[index], testPasswords[index]);
        }

        dbContext.Users.AddRange(testUsers);
        await dbContext.SaveChangesAsync();
    }
}

app.Run();

public partial class Program;
