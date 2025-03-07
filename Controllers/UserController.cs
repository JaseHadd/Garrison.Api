using Garrison.Lib;
using Garrison.Lib.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Garrison.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(GarrisonContext dbContext) : ControllerBase
{
    private readonly GarrisonContext _dbContext = dbContext;

    [Route("id/{id}")]
    public ActionResult<Dictionary<string, object>> GetUser(int id)
    {
        if (_dbContext.Users.Find((uint)id) is User user)
            return new Dictionary<string, object> {
                ["id"] = user.Id,
                ["name"] = user.UserName
            };
        else
            return NotFound();
    }

    [Route("id/{userId}/characters")]
    public IEnumerable<Dictionary<string, object?>> GetCharacters(int userId)
    {
        return _dbContext.Characters
            .Where(c => c.Owner.Id == userId)
            .AsEnumerable()
            .Select(c => new Dictionary<string, object?>() { ["id"] = c.Id, ["foundry_id"] = c.FoundryId, ["name"] = c.Name});
    }

    [Route("me")]
    [Authorize]
    public ActionResult<Dictionary<string, object>> GetSelf()
    {
        HttpContext.Items.TryGetValue("User", out var user);
        return GetUser((int)((User)user!).Id);
    }

    [Authorize]
    [Route("me/adventures")]
    public IEnumerable<Dictionary<string, object?>> GetAdventures()
    {
        User user = (User)HttpContext.Items["User"]!;
        
        return _dbContext.Adventures
            .Where(a => a.GameMaster.Id == user.Id)
            .Include(a => a.Players)
            .ThenInclude(p => p.Player)
            .Include(a => a.Players)
            .ThenInclude(p => p.Character)
            .AsEnumerable()
            .Select(a => new Dictionary<string, object?>() {
                ["id"] = a.Id,
                ["name"] = a.Name,
                ["characters"] = a.Players.Select(p => new Dictionary<string, object?>() {
                    ["id"] = p.Character?.FoundryId,
                    ["player"] = p.Player.UserName,
                    ["name"] = p.Character?.Name
                })
            });
    }
}