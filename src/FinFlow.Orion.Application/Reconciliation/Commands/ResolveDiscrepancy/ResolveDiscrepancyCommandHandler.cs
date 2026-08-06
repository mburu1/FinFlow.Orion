using FinFlow.Orion.Application.Common.Exceptions;
using FinFlow.Orion.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Reconciliation.Commands.ResolveDiscrepancy;

public sealed class ResolveDiscrepancyCommandHandler
    : IRequestHandler<ResolveDiscrepancyCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly ILogger<ResolveDiscrepancyCommandHandler> _logger;

    public ResolveDiscrepancyCommandHandler(
        IUnitOfWork unitOfWork,
        IReconciliationRepository reconciliationRepository,
        ILogger<ResolveDiscrepancyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _reconciliationRepository = reconciliationRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ResolveDiscrepancyCommand request,
        CancellationToken cancellationToken)
    {
        var discrepancy = await _reconciliationRepository
            .GetDiscrepancyByIdAsync(request.DiscrepancyId, cancellationToken)
            ?? throw new NotFoundException(
                nameof(Domain.Entities.Reconciliation.Discrepancy), request.DiscrepancyId);

        discrepancy.Resolve(request.ResolvedBy, request.Notes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Reconciliation] Discrepancy {Id} resolved by {By} — Notes: {Notes}",
            discrepancy.Id,
            request.ResolvedBy,
            request.Notes);

        return true;
    }
}