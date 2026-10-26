using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<QuanLyHocBong_UNETI2_TI17A3HNContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("QuanLyHocBong_UNETI2_TI17A3HNContext") ?? throw new InvalidOperationException("Connection string 'QuanLyHocBong_UNETI2_TI17A3HNContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => { options.Cookie.HttpOnly = true; options.Cookie.IsEssential = true; options.IdleTimeout = TimeSpan.FromMinutes(30); });

builder.Services.AddScoped<IHoSoHocBongService, HoSoHocBongService>();
builder.Services.AddScoped<IHocBongService, HocBongService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<QuanLyHocBong_UNETI2_TI17A3HN.Services.IEmailService, QuanLyHocBong_UNETI2_TI17A3HN.Services.EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

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
