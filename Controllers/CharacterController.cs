using System.Text.Json;
using System.Threading.Tasks;
using Garrison.Lib;
using Garrison.Lib.Models;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;

namespace Garrison.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CharacterController(GarrisonContext dbContext, IFileManager fileManager) : ControllerBase
{
    private readonly GarrisonContext _dbContext = dbContext;
    private readonly IFileManager _fileManager = fileManager;

    [HttpGet("id/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Character> GetCharacter(int id)
    {
        if (_dbContext.Characters.Find(id) is Character @char)
            return @char;
        else
            return NotFound();
    }

    [HttpGet("foundry/{foundryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Character> GetCharacter(string foundryId)
    {
        try
        {
            if (GetCharacter(foundryId, nameof(foundryId)) is Character @char)
                return @char;
            else
                return NotFound();
        } catch (UserError error)
        {
            return BadRequest(error.ProblemDetails);
        }
    }

    [Authorize]
    [HttpPost("foundry/{foundryId}/json")]
    public async Task<IActionResult> SetJson(string foundryId) => await SetFile(foundryId, IFileManager.FileType.Json);

    [Authorize]
    [HttpPut("foundry/{foundryId}/token")]
    public async Task<IActionResult> SetToken(string foundryId) => await SetFile(foundryId, IFileManager.FileType.Token);

    [Authorize]
    [HttpPut("foundry/{foundryId}/portrait")]
    public async Task<IActionResult> SetPortrait(string foundryId) => await SetFile(foundryId, IFileManager.FileType.Portrait);

    [Authorize]
    [HttpGet("foundry/{foundryId}/json")]
    public IActionResult GetJson(string foundryId) => GetFile(foundryId, IFileManager.FileType.Json);

    [Authorize]
    [HttpGet("foundry/{foundryId}/token")]
    public IActionResult GetToken(string foundryId) => GetFile(foundryId, IFileManager.FileType.Token);

    [Authorize]
    [HttpGet("foundry/{foundryId}/portrait")]
    public IActionResult GetPortrait(string foundryId) => GetFile(foundryId, IFileManager.FileType.Portrait);

    private IActionResult GetFile(string foundryId, IFileManager.FileType type)
    {
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");
        
        if (!_fileManager.TryGetFile(@char, type, out var file))
            return NotFound($"No {type} stored for character");

        return File(file.OpenRead(), type.GetMimeType());
    }

    private async Task<IActionResult> SetFile(string foundryId, IFileManager.FileType type)
    {
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");

        await _fileManager.SaveFile(@char, type, Request.Body, Request.ContentLength);
        return Ok();
    }

    private async Task SaveFile(Character @char, IFileManager.FileType type) => await _fileManager.SaveFile(@char, type, Request.Body, Request.ContentLength);

    private Character? GetCharacterFromId(uint id) => _dbContext.Characters.Find(id);
    
    private Character? GetCharacter(string foundryId, string parameterName)
    {
        if (foundryId.Length != 16)
            throw new UserError(StatusCodes.Status400BadRequest, $"{parameterName} must be exactly 16 characters");
        if (!foundryId.All(char.IsAsciiLetterOrDigit))
            throw new UserError(StatusCodes.Status400BadRequest, $"{parameterName} must be alphanumeric");

        return _dbContext.Characters.SingleOrDefault(c => c.FoundryId == foundryId);
    }

    private class UserError(int code, string message) : Exception(message)
    {
        public int              Code { get; }   = code;

        public ProblemDetails   ProblemDetails  => new() { Status = Code, Detail = Message };
    }
}