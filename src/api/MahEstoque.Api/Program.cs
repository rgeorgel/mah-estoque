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

builder.Services.AddHttpContextAccessor();

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

    // Fase 2: campos de venda no produto
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Description"" text");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""SalePrice"" numeric(10,2)");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""SalePriceDiscount"" numeric(10,2)");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""IsVisible"" boolean NOT NULL DEFAULT false");

    // Fase 2: slug e whatsapp no tenant
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Tenants"" ADD COLUMN IF NOT EXISTS ""Slug"" character varying(100)");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Tenants"" ADD COLUMN IF NOT EXISTS ""WhatsappNumber"" character varying(30)");
    db.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Tenants_Slug"" ON ""Tenants"" (""Slug"") WHERE ""Slug"" IS NOT NULL");

    // Fase 1: tabela de imagens de produto
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""ProductImages"" (
            ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
            ""ProductId"" uuid NOT NULL,
            ""TenantId"" uuid NOT NULL,
            ""FileName"" character varying(255) NOT NULL,
            ""StoredPath"" character varying(500) NOT NULL,
            ""IsPrimary"" boolean NOT NULL DEFAULT false,
            ""DisplayOrder"" integer NOT NULL DEFAULT 0,
            ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT ""PK_ProductImages"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_ProductImages_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
        )");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_ProductImages_ProductId"" ON ""ProductImages"" (""ProductId"")");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_ProductImages_TenantId"" ON ""ProductImages"" (""TenantId"")");

    // Fase 4: tabelas de pedidos
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Orders"" (
            ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
            ""TenantId"" uuid NOT NULL,
            ""Status"" character varying(20) NOT NULL DEFAULT 'Pending',
            ""CustomerName"" character varying(255),
            ""CustomerPhone"" character varying(30),
            ""TotalValue"" numeric(10,2) NOT NULL DEFAULT 0,
            ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT ""PK_Orders"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_Orders_Tenants_TenantId"" FOREIGN KEY (""TenantId"") REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
        )");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Orders_TenantId"" ON ""Orders"" (""TenantId"")");
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""OrderItems"" (
            ""Id"" uuid NOT NULL DEFAULT gen_random_uuid(),
            ""OrderId"" uuid NOT NULL,
            ""ProductId"" uuid NOT NULL,
            ""VariantId"" uuid,
            ""Quantity"" integer NOT NULL DEFAULT 1,
            ""UnitPrice"" numeric(10,2) NOT NULL DEFAULT 0,
            CONSTRAINT ""PK_OrderItems"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_OrderItems_Orders_OrderId"" FOREIGN KEY (""OrderId"") REFERENCES ""Orders"" (""Id"") ON DELETE CASCADE,
            CONSTRAINT ""FK_OrderItems_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT,
            CONSTRAINT ""FK_OrderItems_Variants_VariantId"" FOREIGN KEY (""VariantId"") REFERENCES ""ProductVariants"" (""Id"") ON DELETE SET NULL
        )");
    db.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_OrderItems_OrderId"" ON ""OrderItems"" (""OrderId"")");

    // Forma de pagamento e parcelas nas transações
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Transactions"" ADD COLUMN IF NOT EXISTS ""PaymentMethod"" character varying(20)");
    db.Database.ExecuteSqlRaw(@"ALTER TABLE ""Transactions"" ADD COLUMN IF NOT EXISTS ""Installments"" integer");
}

// Serve uploaded images as static files from /uploads/
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();