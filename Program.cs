using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<QuanLyHocBong_UNETI2_TI17A3HNContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyHocBong_UNETI2_TI17A3HNContext") ?? throw new InvalidOperationException("Connection string 'QuanLyHocBong_UNETI2_TI17A3HNContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    var p = options.ModelBindingMessageProvider;
    p.SetValueMustBeANumberAccessor(name => $"{name} phải là một số (ví dụ 3.25).");
    p.SetValueIsInvalidAccessor(v => $"Giá trị '{v}' không hợp lệ.");
    p.SetAttemptedValueIsInvalidAccessor((v, name) => $"Giá trị '{v}' không hợp lệ cho {name}.");
    p.SetUnknownValueIsInvalidAccessor(name => $"Giá trị không hợp lệ cho {name}.");
    p.SetValueMustNotBeNullAccessor(_ => "Vui lòng nhập giá trị.");
    p.SetMissingBindRequiredValueAccessor(name => $"Thiếu giá trị cho {name}.");
    p.SetMissingKeyOrValueAccessor(() => "Vui lòng nhập giá trị.");
    p.SetMissingRequestBodyRequiredValueAccessor(() => "Dữ liệu gửi lên không hợp lệ.");
    p.SetNonPropertyAttemptedValueIsInvalidAccessor(v => $"Giá trị '{v}' không hợp lệ.");
    p.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Giá trị không hợp lệ.");
    p.SetNonPropertyValueMustBeANumberAccessor(() => "Giá trị phải là một số.");
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

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
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
