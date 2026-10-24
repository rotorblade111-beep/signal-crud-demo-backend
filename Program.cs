using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Signal_CRUD_Demo_Backend.Data;
using Signal_CRUD_Demo_Backend.Models;
using Signal_CRUD_Demo_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

var signingKey = builder.Configuration["Authentication:Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "Configure Authentication:Jwt:SigningKey with a secret of at least 32 UTF-8 bytes.");
if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException(
        "Authentication:Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
}

builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AccessTokenService.Issuer,
            ValidateAudience = true,
            ValidAudience = AccessTokenService.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
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
builder.Services.AddSingleton<IAccessTokenService>(_ => new AccessTokenService(signingKey));

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
    var connection = dbContext.Database.GetDbConnection();
    await connection.OpenAsync();
    var hasCreatedAt = false;
    await using (var command = connection.CreateCommand())
    {
        command.CommandText = "PRAGMA table_info('Posts');";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), "CreatedAt", StringComparison.OrdinalIgnoreCase))
            {
                hasCreatedAt = true;
                break;
            }
        }
    }

    if (!hasCreatedAt)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE "Posts"
            ADD COLUMN "CreatedAt" TEXT NOT NULL DEFAULT '1970-01-01T00:00:00.000+00:00';
            UPDATE "Posts"
            SET "CreatedAt" = strftime(
                '%Y-%m-%dT%H:%M:%f+00:00',
                'now',
                printf('-%d days', "Id"))
            WHERE "CreatedAt" = '1970-01-01T00:00:00.000+00:00';
            """);
    }

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
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "RefreshTokens" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_RefreshTokens" PRIMARY KEY AUTOINCREMENT,
            "UserId" INTEGER NOT NULL,
            "TokenHash" TEXT NOT NULL,
            "ExpiresAt" TEXT NOT NULL,
            "RevokedAt" TEXT NULL,
            CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
        CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
        """);

    if (!await dbContext.Posts.AnyAsync())
    {
        var seededAt = DateTimeOffset.UtcNow;
        dbContext.Posts.AddRange(Enumerable.Range(1, 100).Select(id => new PostEntity
        {
            Title = $"Post {id}",
            Body = $"This is the body of post {id}.",
            CreatedAt = seededAt.AddDays(-id)
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
