using E_commerce.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Create the application builder - this sets up configuration, logging and DI
var builder = WebApplication.CreateBuilder(args);

// Read the database connection string from appsettings.json
// Throws immediately at startup if the key is missing so the error is obvious
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register the EF Core database context with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Adds a helpful error page in development when a pending migration is detected
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Set up ASP.NET Core Identity with roles support.
// RequireConfirmedAccount = false so seeded users (and new registrations) can log in
// straight away without needing to click an email confirmation link.
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit           = true;
    options.Password.RequireLowercase       = true;
    options.Password.RequireUppercase       = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength         = 8;
})
    .AddRoles<IdentityRole>()                          // Enable role-based authorisation
    .AddEntityFrameworkStores<ApplicationDbContext>();  // Store identity data in our SQL database

// Register the MVC controllers and views pipeline
builder.Services.AddControllersWithViews();

// Only register Google OAuth if both credentials are present in config.
// Leaving them blank in appsettings disables Google login without crashing the app.
var googleClientId     = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId     = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
}

var app = builder.Build();

// Run database seeding inside a scoped service block at startup.
// This creates default roles, users, producers and products if they do not already exist.
using (var scope = app.Services.CreateScope())
{
    var services    = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedData.SeedUsersAndRoles(services, userManager, roleManager);
    await SeedData.SeedProducers(services);
    await SeedData.SeedProducts(services);
}

// Configure the HTTP request pipeline differently depending on the environment
if (app.Environment.IsDevelopment())
{
    // Show the migration end-point page in development to help with database changes
    app.UseMigrationsEndPoint();
}
else
{
    // In production, redirect unhandled exceptions to a friendly error page
    app.UseExceptionHandler("/Home/Error");
    // HSTS tells browsers to only connect over HTTPS for the next year
    app.UseHsts();
}

app.UseHttpsRedirection(); // Redirect any HTTP requests to HTTPS
app.UseRouting();          // Enable attribute and conventional routing

app.UseAuthentication();   // Must come before UseAuthorization - reads the auth cookie/token
app.UseAuthorization();    // Checks the user has permission to access the requested resource
app.MapStaticAssets();     // Serve wwwroot files (CSS, JS, images) efficiently

// Default MVC route: /Controller/Action/id
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map Razor Pages - used by ASP.NET Identity's built-in login/register/manage pages
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
