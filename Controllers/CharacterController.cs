using System.Text.Json;
using Garrison.Lib;
using Garrison.Lib.Models;
using Microsoft.AspNetCore.Mvc;

namespace Garrison.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CharacterController(GarrisonContext dbContext) : ControllerBase
{
    private readonly GarrisonContext _dbContext = dbContext;

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