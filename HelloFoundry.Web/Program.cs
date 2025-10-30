using HelloFoundry.Web.Services;
using HelloFoundry.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<AiModelOptions>(
    builder.Configuration.GetSection(AiModelOptions.SectionName));
builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection(ApiOptions.SectionName));

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddControllers(options =>
{
    // Add model validation
    options.ModelValidatorProviders.Clear();
});

// Model validation is enabled by default in ASP.NET Core

// Register AI service
builder.Services.AddSingleton<IAiChatService, AiChatService>();

// Add security headers
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();