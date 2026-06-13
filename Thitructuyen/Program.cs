
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Thitructuyen.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

var app = builder.Build();

// Tự áp dụng migration + seed dữ liệu mẫu
// Đồng thời tự vá các cột mới để tránh lỗi khi copy đè nhưng chưa kịp chạy Update-Database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        db.Database.Migrate();
    }
    catch
    {
        // Nếu migration cũ trong máy bị lệch, vẫn cho app chạy để phần SQL vá cột bên dưới xử lý.
        // Trường hợp database chưa tồn tại hoàn toàn thì người dùng chạy Update-Database lại trong Package Manager Console.
    }

    try
    {
        db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID(N'[dbo].[Exams]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.Exams', N'ExamPassword') IS NULL
BEGIN
    ALTER TABLE [dbo].[Exams] ADD [ExamPassword] NVARCHAR(100) NULL;
END

IF OBJECT_ID(N'[dbo].[ExamAttempts]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.ExamAttempts', N'ViolationCount') IS NULL
BEGIN
    ALTER TABLE [dbo].[ExamAttempts] ADD [ViolationCount] INT NOT NULL CONSTRAINT DF_ExamAttempts_ViolationCount DEFAULT 0;
END

IF OBJECT_ID(N'[dbo].[ExamAttempts]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.ExamAttempts', N'ViolationLog') IS NULL
BEGIN
    ALTER TABLE [dbo].[ExamAttempts] ADD [ViolationLog] NVARCHAR(MAX) NULL;
END

IF OBJECT_ID(N'[dbo].[ExamAttempts]', N'U') IS NOT NULL
AND COL_LENGTH(N'dbo.ExamAttempts', N'IpAddress') IS NULL
BEGIN
    ALTER TABLE [dbo].[ExamAttempts] ADD [IpAddress] NVARCHAR(MAX) NULL;
END

IF OBJECT_ID(N'[dbo].[ActivityLogs]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ActivityLogs](
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ActivityLogs] PRIMARY KEY,
        [UserId] INT NULL,
        [Action] NVARCHAR(100) NOT NULL,
        [Detail] NVARCHAR(500) NOT NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
");
    }
    catch
    {
        // Không chặn khởi động app nếu database local chưa sẵn sàng.
    }

    DbInitializer.Seed(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
