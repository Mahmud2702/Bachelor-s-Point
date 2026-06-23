using Bachelor_s_Point.Application.Interfaces.Repositories;
using Bachelor_s_Point.Application.Interfaces.Services;
using Bachelor_s_Point.Application.Interfaces.UnitOfWork;
using Bachelor_s_Point.Data;
using Bachelor_s_Point.Infrastructure.Email;
using Bachelor_s_Point.Infrastructure.Settings;
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
        options.LoginPath         = "/Auth/Login";
        options.AccessDeniedPath  = "/Auth/AccessDenied";
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// HttpClient for SSLCommerz
builder.Services.AddHttpClient<ISSLCommerzService, SSLCommerzService>();

// ── Repositories ────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository,               UserRepository>();
builder.Services.AddScoped<IRoleRepository,               RoleRepository>();
builder.Services.AddScoped<IAdminRepository,              AdminRepository>();
builder.Services.AddScoped<IRoomRepository,               RoomRepository>();
builder.Services.AddScoped<IRoomSelectionRepository,      RoomSelectionRepository>();
builder.Services.AddScoped<IRoomImageRepository,          RoomImageRepository>();
builder.Services.AddScoped<IChatRepository,               ChatRepository>();
builder.Services.AddScoped<IPendingRegistrationRepository,PendingRegistrationRepository>();
builder.Services.AddScoped<IPasswordResetRepository,      PasswordResetRepository>();
builder.Services.AddScoped<IKycRepository,                KycRepository>();
builder.Services.AddScoped<ILoginHistoryRepository,       LoginHistoryRepository>();
builder.Services.AddScoped<IPaymentRepository,            PaymentRepository>();

// ── Unit of Work ────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, Bachelor_s_Point.UnitOfWork.UnitOfWork>();

// ── Services ────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IUserService,    UserService>();
builder.Services.AddScoped<IRoleService,    RoleService>();
builder.Services.AddScoped<IRoomService,    RoomService>();
builder.Services.AddScoped<IKycService,     KycService>();
builder.Services.AddScoped<IChatService,    ChatService>();
builder.Services.AddScoped<IEmailService,   EmailService>();
builder.Services.AddScoped<ISmtpEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ── Settings ────────────────────────────────────────────────
builder.Services.Configure<SmtpOptions>(        builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<PaymentSettings>(    builder.Configuration.GetSection("Payment"));
builder.Services.Configure<SSLCommerzSettings>( builder.Configuration.GetSection("SSLCommerz"));

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
