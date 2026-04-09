using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MahEstoque.Api.Data;
using MahEstoque.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "MahEstoqueSecretKey2026VeryLongAndSecure!";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Database=mahestoque;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "MahEstoque",
            ValidAudience = "MahEstoque",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReportService, ReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("ALTER TABLE \"Products\" ADD COLUMN IF NOT EXISTS \"Size\" character varying(50)");
    db.Database.ExecuteSqlRaw("ALTER TABLE \"Products\" ALTER COLUMN \"SKU\" DROP NOT NULL");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ProductVariants"" (
            ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
            ""ProductId"" uuid NOT NULL,
            ""TenantId"" uuid NOT NULL,
            ""Size"" character varying(50),
            ""Color"" character varying(50),
            ""SKU"" character varying(50),
            ""Quantity"" integer NOT NULL DEFAULT 0,
            ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT ""PK_ProductVariants"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_ProductVariants_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_ProductVariants_Tenants_TenantId"" FOREIGN KEY (""TenantId"") REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
        )");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_ProductVariants_ProductId"" ON ""ProductVariants"" (""ProductId"")");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_ProductVariants_TenantId"" ON ""ProductVariants"" (""TenantId"")");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Transactions"" ADD COLUMN IF NOT EXISTS ""VariantId"" uuid REFERENCES ""ProductVariants""(""Id"") ON DELETE SET NULL");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();