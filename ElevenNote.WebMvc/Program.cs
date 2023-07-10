using ElevenNote.Data;
using ElevenNote.Data.Entities;
using ElevenNote.Services.Note;
using ElevenNote.Services.User;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// DbContext configuration, adds the DbContext for dependency injection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Service dependency injection configuration
// Allows controllers to have services injected
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INoteService, NoteService>();

// Enables using Identity Managers (Users, SignIn, Password)
builder.Services.AddDefaultIdentity<UserEntity>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Configure what happens when a logged out user tries to access an authorized route
builder.Services.ConfigureApplicationCookie(options => 
{
    options.LoginPath = "/Account/Login";
});

builder.Services.AddControllersWithViews();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();