using QuanLyHocBong_UNETI2_TI17A3HN.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Nạp biến môi trường (.env) & Đăng ký các tầng dịch vụ (Clean Modular DI)
builder.AddDotEnvConfiguration();

builder.Services
    .AddPersistenceLayer(builder.Configuration)
    .AddApplicationServices();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. Tự động áp dụng Database Migration khi khởi chạy
app.UseDatabaseMigration();

// 3. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
app.UseMiddleware<JwtAuthMiddleware>(); // Xác minh JWT AccessToken & tự động làm mới bằng RefreshToken trong DB
app.UseAuthorization();

// 4. Ánh xạ Route MVC & Static Assets
app.MapApplicationEndpoints();

app.Run();
