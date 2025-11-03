using MarathonManager.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies; // <--- 1. THÊM USING NÀY

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("MarathonApi", client =>
{
    // Địa chỉ API của bạn (lấy từ launchSettings.json của API)
    client.BaseAddress = new Uri("https://localhost:7280");
});

builder.Services.AddHttpClient<IRunnerApiService, RunnerApiService>(client =>
{
    // Set the base address of your API project
    client.BaseAddress = new Uri("https://localhost:7280");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// <--- 2. THÊM KHỐI CẤU HÌNH COOKIE VÀ HTTPCONTEXT ---
// 1. Cấu hình dịch vụ Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Đường dẫn đến trang đăng nhập
        options.LogoutPath = "/Account/Logout"; // Đường dẫn để đăng xuất
        options.AccessDeniedPath = "/Account/AccessDenied"; // Trang cấm truy cập
        options.ExpireTimeSpan = TimeSpan.FromHours(24); // Thời gian hết hạn cookie
        options.SlidingExpiration = true; // Tự động gia hạn
    });

// 2. Dịch vụ để đọc/ghi HttpContext (cần để truy cập Cookies)
builder.Services.AddHttpContextAccessor();

// Configure session (if needed for temporary data)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// --- KẾT THÚC KHỐI THÊM ---
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// <--- 3. THÊM DÒNG NÀY (RẤT QUAN TRỌNG) ---
// Phải nằm TRƯỚC app.UseAuthorization()
app.UseAuthentication();
// --- KẾT THÚC DÒNG THÊM ---

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();