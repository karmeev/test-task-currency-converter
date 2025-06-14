using Autofac;
using Autofac.Extensions.DependencyInjection;
using Currency.Api;
using Currency.Api.BackgroundServices;
using Currency.Api.Configurations;
using Currency.Api.Middlewares;
using Currency.Api.ModelBinders;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddSettings(builder.Environment.EnvironmentName, out var settings);
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new CustomModelBinderProvider());
});
builder.Services.AddVersioning();
builder.Services.AddRateLimiter(settings);
builder.Services.AddIdentity(settings);
builder.Services.AddCustomBehavior();
builder.Services.AddThirdPartyApis(settings);
builder.Services.AddHostedService<ConsumersStartupBackgroundService>();
builder.Services.AddTransient<ExceptionHandler>();
builder.Host.AddLogger(builder.Services, builder.Environment.EnvironmentName);

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    Registry.RegisterDependencies(container, builder.Configuration);
});

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCustomExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseRequestLogging();
app.Run();