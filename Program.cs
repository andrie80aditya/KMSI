using Microsoft.EntityFrameworkCore;
using KMSI.Data;
using KMSI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<KMSIDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add these lines in Program.cs
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddTransient<IPdfService, PdfService>();
builder.Services.AddTransient<IEmailService, EmailService>();

// Add Authentication & Authorization services
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Define authorization policies based on UserLevel
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireClaim("UserLevel", "SUPER"));

    options.AddPolicy("HOAdminAndAbove", policy =>
        policy.RequireClaim("UserLevel", "SUPER", "HO_ADMIN"));

    options.AddPolicy("BranchManagerAndAbove", policy =>
        policy.RequireClaim("UserLevel", "SUPER", "HO_ADMIN", "BRANCH_MGR"));

    options.AddPolicy("TeacherAndAbove", policy =>
        policy.RequireClaim("UserLevel", "SUPER", "HO_ADMIN", "BRANCH_MGR", "TEACHER"));

    options.AddPolicy("AllUsers", policy =>
        policy.RequireClaim("UserLevel", "SUPER", "HO_ADMIN", "BRANCH_MGR", "TEACHER", "STAFF"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();