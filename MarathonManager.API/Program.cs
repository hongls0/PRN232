using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==========================================================
// 1. SỬA LỖI CORS (ĐỔI PORT)
// ==========================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        // Sửa 7280 thành port của dự án WEB (Frontend)
        policy.WithOrigins("https://localhost:7281")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();

        // Hoặc nếu có nhiều địa chỉ Web (ví dụ port http):
        // policy.WithOrigins("https://localhost:7281", "http://localhost:5001")
        //       .AllowAnyHeader()
        //       .AllowAnyMethod()
        //       .AllowCredentials();
    });
});
// ==========================================================

// Cấu hình Swagger (code của bạn đã RẤT TỐT, giữ nguyên)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MarathonManager API",
        Version = "v1"
    });

    // Dòng này sửa lỗi 500 (trùng DTO)
    c.CustomSchemaIds(type => type.FullName);

    // Thêm cấu hình để Swagger hiển thị nút Authorize
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập token theo định dạng: Bearer {your token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("MyCnn");

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<MarathonManagerContext>(options =>
    options.UseSqlServer(connectionString));

// 1. Cấu hình Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MarathonManagerContext>()
.AddDefaultTokenProviders();

// 2. Cấu hình JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// ==========================================================
// BẮT ĐẦU CẤU HÌNH PIPELINE (MIDDLEWARE)
// ==========================================================
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. Dùng HTTPS Redirection
app.UseHttpsRedirection();

// 2. Dùng Static Files (cho ảnh upload)
// (XÓA DÒNG app.UseStaticFiles(); ở trên cùng)
app.UseStaticFiles();

// 3. (Tùy chọn) Thêm UseRouting để đảm bảo thứ tự
app.UseRouting();

// 4. SỬA LỖI VỊ TRÍ: Đặt UseCors ở đây
// (Sau UseRouting, trước UseAuthentication/UseAuthorization)
app.UseCors("AllowWeb");

// 5. Dùng Authentication
app.UseAuthentication();

// 6. Dùng Authorization
app.UseAuthorization();

// 7. Map Controllers
app.MapControllers();

app.Run();