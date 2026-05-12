using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Infrastructure.Email;
using Bachelor_s_Point.Repositories;
using Bachelor_s_Point.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomSelectionRepository, RoomSelectionRepository>();
builder.Services.AddScoped<IRoomImageRepository, RoomImageRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddScoped<IUnitOfWork, Bachelor_s_Point.UnitOfWork.UnitOfWork>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmtpEmailService, SmtpEmailService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
