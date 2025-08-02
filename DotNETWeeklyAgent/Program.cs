using DotNETWeeklyAgent;
using DotNETWeeklyAgent.Extensions;
using DotNETWeeklyAgent.Models;
using DotNETWeeklyAgent.Options;
using DotNETWeeklyAgent.Services;
using DotNETWeeklyAgent.Services.HostedServices;
using DotNETWeeklyAgent.SK;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMvc();

#if DEBUG
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});
#endif
builder.Services.AddLogging(config => config.AddDebug().SetMinimumLevel(LogLevel.Information));
builder.Services.AddSingleton<IBackgroundTaskQueue<IssueMetadata>, BackgroundTaskQueue<IssueMetadata>>();
builder.Services.AddSingleton<IBackgroundTaskQueue<MilestoneMetadata>, BackgroundTaskQueue<MilestoneMetadata>>();
builder.Services.AddHostedService<GithubIssueHostedService>();
builder.Services.AddHostedService<GithubMilestoneHostedService>();
builder.Services.Configure<AzureOpenAIOptions>(builder.Configuration.GetSection("AzureOpenAI"));
builder.Services.Configure<GithubOptions>(builder.Configuration.GetSection("Github"));
builder.Services.AddGithubAPIHttpClient()
    .AddWebContentHttpClient();
builder.Services.AddIssueSemanticKernal();
builder.Services.AddMilestoneSemanticKernal();
builder.Services.AddSingleton<ISecretTokenValidator, SecretTokenValidator>();
builder.Services.AddTransient<SecretTokenValidationMiddleware>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseMiddleware<SecretTokenValidationMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
//app.UseMiddleware<SecretTokenValidationMiddleware>();

#if DEBUG
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "My API V1");
});
#endif

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
