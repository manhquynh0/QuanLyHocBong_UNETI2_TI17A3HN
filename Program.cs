using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QuanLyHocBong_UNETI2_TI17A3HNContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyHocBong_UNETI2_TI17A3HNContext") ?? throw new InvalidOperationException("Connection string 'QuanLyHocBong_UNETI2_TI17A3HNContext' not found."))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký Session (bắt buộc khi dùng app.UseSession())
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== Đăng ký các Service (Dependency Injection) =====
// Module 3 - Học bổng (sinh viên xem)
builder.Services.AddScoped<IHocBongService, HocBongService>();

// Module 4 - Hồ sơ học bổng
builder.Services.AddScoped<IHoSoHocBongService, HoSoHocBongService>();

// Module Email (dùng bởi KetQuaHocBong & LichPhongVan)
builder.Services.AddScoped<IEmailService, EmailService>();

// HttpClient & Cloud Storage Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

var app = builder.Build();

// Tự động áp dụng Migration khi khởi động ứng dụng để các thành viên khác khi kéo code về chạy được ngay
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<QuanLyHocBong_UNETI2_TI17A3HNContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình tự động cập nhật Database Migration.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var enUS = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(enUS),
    SupportedCultures = new[] { enUS },
    SupportedUICultures = new[] { enUS }
});

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound");

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TaiKhoan}/{action=DangNhap}/{id?}")
    .WithStaticAssets();

app.Run();
