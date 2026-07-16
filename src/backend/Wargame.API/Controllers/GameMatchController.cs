using MediatR;
using Microsoft.AspNetCore.Mvc;
using Wargame.Application.Commands.Activation;
using Wargame.Application.Commands.GameMatch;
using Wargame.Application.Commands.Movement;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Commands.Shooting;
using Wargame.Application.Commands.Shooting.DTOs;
using Wargame.Application.Queries.GameMatch;

namespace Wargame.API.Controllers;

[ApiController]
[Route("api/matches")]
public class GameMatchController : ControllerBase
{
    private readonly ISender _sender;

    public GameMatchController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGameMatch([FromBody] CreateGameMatchCommand command, CancellationToken cancellationToken)
    {
        var matchId = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetGameMatch), new { id = matchId }, new { Id = matchId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetGameMatch(Guid id, CancellationToken cancellationToken)
    {
        var match = await _sender.Send(new GetGameMatchQuery(id), cancellationToken);
        
        if (match == null)
            return NotFound();
            
        return Ok(match);
    }

    [HttpPost("{id:guid}/roll-initiative")]
    public async Task<IActionResult> RollInitiative(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new RollInitiativeCommand(id), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/activate-unit")]
    public async Task<IActionResult> ActivateUnit(Guid id, [FromBody] ActivateUnitRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new ActivateUnitCommand(id, request.UnitId), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/advance-turn")]
    public async Task<IActionResult> AdvanceTurn(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new AdvanceTurnCommand(id), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/move-unit")]
    public async Task<IActionResult> MoveUnit(Guid id, [FromBody] MoveUnitRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new MoveUnitCommand(id, request.UnitId, request.MovementType, request.FigureMoves), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/declare-stationary")]
    public async Task<IActionResult> DeclareStationary(Guid id, [FromBody] DeclareStationaryRequest request, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeclareStationaryCommand(id, request.UnitId), cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/shoot-unit")]
    public async Task<IActionResult> ShootUnit(Guid id, [FromBody] ShootUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ShootUnitCommand(id, request.ShootingUnitId, request.FigureShots), cancellationToken);
        return Ok(result);
    }
}

/// <summary>
/// DTO pour la requête d'activation d'unité (évite d'envoyer l'Id de la partie dans le body alors qu'il est dans l'URL).
/// </summary>
public record ActivateUnitRequest(Guid UnitId);

/// <summary>
/// DTO pour la requête de mouvement d'unité.
/// </summary>
public record MoveUnitRequest(Guid UnitId, Wargame.Domain.Enums.MovementType MovementType, List<FigureMoveDto> FigureMoves);

/// <summary>
/// DTO pour la déclaration d'immobilité.
/// </summary>
public record DeclareStationaryRequest(Guid UnitId);

/// <summary>
/// DTO pour la requête de tir.
/// </summary>
public record ShootUnitRequest(Guid ShootingUnitId, List<FigureShootDto> FigureShots);

