using System.Text.Json.Serialization;
using Garrison.Lib;
using Garrison.Lib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();


        if (builder.Configuration.GetConnectionString("Default") is null)
        {
            Console.WriteLine("Please set the connection string in user secrets");
            Console.WriteLine("Usage: dotnet user-secrets set ConnectionStrings:Default \"server=....\"");
            return;
        }
        
        ConfigureServices(builder);


        var app = builder.Build();
        app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader());
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
        => ConfigureServices(builder.Services, builder.Configuration);

    private static void ConfigureServices(IServiceCollection services, ConfigurationManager config)
    {
        services.AddDbContext<GarrisonContext>(options => options.UseMySQL(config.GetConnectionString("Default")!));
        services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
        services.AddOpenApi();
    }
}

public class AuthorizeAttribute() : TypeFilterAttribute(typeof(AuthorizeActionFilter))
{
}

public class AuthorizeActionFilter(GarrisonContext dbContext) : IAsyncActionFilter
{
    private readonly GarrisonContext _dbContext = dbContext;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authValues = context.HttpContext.Request.Headers.Authorization;

        if (authValues.Count != 1)
            context.Result = new BadRequestObjectResult("Missing or duplicate Authorization Header");
        else if (authValues.First()!.Split(' ') is [var type, var token])
        {
            if (type is "Bearer" && token.Length is 32)
            {
                var results = _dbContext.ApiKeys
                        .Where(k => k.Key == token)
                        .Include(k => k.Owner);

                // Expiry check later on.
                if (await results.FirstAsync() is ApiKey key)
                {
                    var user = key.Owner;
                    context.HttpContext.Items.Add(new("User", user));
                    await next();
                }
                else
                {
                    context.Result = new BadRequestObjectResult("Invalid Bearer token");
                }
            }
            else
            {
                context.Result = new BadRequestObjectResult("Invalid Authorization Header");
            }
        }
        else
            context.Result = new BadRequestObjectResult("Invalid Authorization Header");

    }
}