using Asp.Versioning;
using FinFlow.Orion.Application;
using FinFlow.Orion.Application.Payments.Commands.InitiatePayment;
using FinFlow.Orion.Application.Payments.Commands.RetryPayment;
using FinFlow.Orion.Application.Payments.Commands.ReversePayment;
using FinFlow.Orion.Application.Payments.Queries.GetPaymentById;
using FinFlow.Orion.Application.Payments.Queries.GetPaymentsByCustomer;
using FinFlow.Orion.Contracts.Common;
using FinFlow.Orion.Contracts.Payments.Requests;
using FinFlow.Orion.Contracts.Payments.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinFlow.Orion.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
        => _mediator = mediator;

    /// <summary>Initiates a new payment via the specified provider.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InitiatePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetPaymentById),
            new { id = response.PaymentId },
            response);
    }

    /// <summary>Retrieves a payment by its ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>Retrieves paginated payments for a customer.</summary>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(PagedResponse<PaymentSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsByCustomer(
        string customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetPaymentsByCustomerQuery(customerId, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Retries a failed payment, optionally routing to a fallback provider.</summary>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(InitiatePaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RetryPayment(
        Guid id,
        [FromBody] RetryPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RetryPaymentCommand(
            id,
            request.IdempotencyKey,
            request.OverrideProvider);
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Reverses a captured payment.</summary>
    [HttpPost("{id:guid}/reverse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReversePayment(
        Guid id,
        [FromBody] ReversePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var requestedBy = User.Identity?.Name ?? "system";
        var command = request.ToCommand(requestedBy);
        var result = await _mediator.Send(
            new ReversePaymentCommand(id, command.Reason, command.RequestedBy, command.IdempotencyKey),
            cancellationToken);
        return Ok(new { success = result });
    }
}