using Asp.Versioning;
using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Reconciliation.Commands.ResolveDiscrepancy;
using FinFlow.Orion.Application.Reconciliation.Commands.TriggerReconciliation;
using FinFlow.Orion.Application.Reconciliation.Queries.GetDiscrepancies;
using FinFlow.Orion.Application.Reconciliation.Queries.GetReconciliationReport;
using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Reconciliation.Requests;
using FinFlow.Orion.Contracts.Reconciliation.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Orion.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reconciliation")]
[Authorize]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReconciliationController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Manually triggers a reconciliation run for a provider and date.</summary>
    [HttpPost("trigger")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerReconciliation(
        [FromBody] TriggerReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var reportId = await _mediator.Send(command, cancellationToken);
        return AcceptedAtAction(
            nameof(GetReconciliationReport),
            new { reportId },
            new { reportId });
    }

    /// <summary>Retrieves a reconciliation report by ID.</summary>
    [HttpGet("{reportId:guid}")]
    [ProducesResponseType(typeof(ReconciliationReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReconciliationReport(
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetReconciliationReportQuery(reportId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns paginated discrepancies for a report.</summary>
    [HttpGet("{reportId:guid}/discrepancies")]
    [ProducesResponseType(typeof(PagedResponse<DiscrepancyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscrepancies(
        Guid reportId,
        [FromQuery] bool unresolvedOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetDiscrepanciesQuery(reportId, unresolvedOnly, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Resolves a specific discrepancy manually.</summary>
    [HttpPost("discrepancies/{discrepancyId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveDiscrepancy(
        Guid discrepancyId,
        [FromBody] ResolveDiscrepancyRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedBy = User.Identity?.Name ?? "system";
        var result = await _mediator.Send(
            new ResolveDiscrepancyCommand(discrepancyId, resolvedBy, request.Notes),
            cancellationToken);
        return Ok(new { success = result });
    }
}