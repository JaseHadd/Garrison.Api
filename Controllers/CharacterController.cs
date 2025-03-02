using System.Text.Json;
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

    [HttpGet("foundry/{foundryId}/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesErrorResponseType(typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<JsonDocument> GetJson(string foundryId)
    {
        try
        {
            if (GetCharacter(foundryId, nameof(foundryId)) is Character @char
                && @char.Data is string json)
                return JsonDocument.Parse(json);
            else
                return NotFound();
        } catch (UserError error)
        {
            return BadRequest(error.ProblemDetails);
        }
    }

    [Authorize]
    [HttpPut("foundry/{foundryId}/token")]
    public async Task<IActionResult> SetToken(string foundryId)
    {
        if (Request.ContentLength > 2*1024*1024)
            return BadRequest("Images must be <= 2MB");
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");

        await SaveImage(@char, IFileManager.FileType.Token);

        return Ok();
    }

    [Authorize]
    [HttpPut("foundry/{foundryId}/portrait")]
    public async Task<IActionResult> SetPortrait(string foundryId)
    {
        if (Request.ContentLength > 5*1024*1024)
            return BadRequest("Images must be <= 5MB");
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");

        await SaveImage(@char, IFileManager.FileType.Portrait);

        return Ok();
    }

    [Authorize]
    [HttpGet("foundry/{foundryId}/token")]
    public IActionResult GetToken(string foundryId)
    {
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");

        if (_fileManager.TryGetFile(@char, IFileManager.FileType.Token, out var file))
            return File(file.OpenRead(), "image/webp");

        return NotFound("No token found");
    }

    [Authorize]
    [HttpGet("foundry/{foundryId}/portrait")]
    public IActionResult GetPortrait(string foundryId)
    {
        if (GetCharacter(foundryId, nameof(foundryId)) is not Character @char)
            return NotFound("No such character");

        if (_fileManager.TryGetFile(@char, IFileManager.FileType.Portrait, out var file))
            return File(file.OpenRead(), "image/webp");

        return NotFound("No portrait found");
    }

    private async Task SaveImage(Character @char, IFileManager.FileType type)
    {
        var bytes = new byte[Convert.ToInt32(Request.ContentLength)];
        await Request.Body.ReadExactlyAsync(bytes);

        MagickImage image = new(bytes, MagickFormat.Unknown);
        MagickGeometry size = type switch
        {
            IFileManager.FileType.Token => new(400, 400) { FillArea = true },
            IFileManager.FileType.Portrait => new() { Width = 1024 },
            _ => throw new NotImplementedException()
        };

        image.Format = MagickFormat.WebP;
        image.Resize(size);

        await _fileManager.SaveFile(@char, type, image.ToByteArray());

        return;
    }

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