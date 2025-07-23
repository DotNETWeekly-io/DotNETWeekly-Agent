using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;
using DotNETWeeklyAgent.SK;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IBackgroundTaskQueue<IssueMetadata>, BackgroundTaskQueue<IssueMetadata>>();
builder.Services.AddHostedService<GithubIssueOpenHostedService>();
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<GithubOptions>(builder.Configuration.GetSection("Github"));
builder.Services.AddSemanticKernal();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
