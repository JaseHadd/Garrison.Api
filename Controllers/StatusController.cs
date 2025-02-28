using Garrison.Lib;
using Microsoft.AspNetCore.Mvc;

namespace Garrison.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class StatusController(GarrisonContext dbContext)
{
    private readonly GarrisonContext _dbContext = dbContext;

    [Route("database")]
    public Dictionary<string, int> GetDatabaseStatus()
    {
        return new()
        {
            ["Users"] = _dbContext.Users.Count(),
            ["Characters"] = _dbContext.Characters.Count(),
            ["Adventures"] = _dbContext.Adventures.Count()
        };
    }
}