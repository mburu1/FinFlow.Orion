using FinFlow.Orion.Application.Common.Interfaces;
using FinFlow.Orion.Domain.Entities.Reconciliation;
using FinFlow.Orion.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Application.Reconciliation.Commands.TriggerReconciliation;

public sealed class TriggerReconciliationCommandHandler
    : IRequestHandler<TriggerReconciliationCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReconciliationRepository _reconciliationRepository;
    private readonly ILogger<TriggerReconciliationCommandHandler> _logger;

    public TriggerReconciliationCommandHandler(
        IUnitOfWork unitOfWork,
        IReconciliationRepository reconciliationRepository,
        ILogger<TriggerReconciliationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _reconciliationRepository = reconciliationRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        TriggerReconciliationCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<PaymentProvider>(request.Provider, true, out var provider))
            throw new ArgumentException($"Unsupported provider: {request.Provider}");

        var report = ReconciliationReport.Create(provider, request.ReconDate);

        await _reconciliationRepository.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[Reconciliation] Triggered for {Provider} on {Date} by {By} — Report: {Ref}",
            provider, request.ReconDate, request.TriggeredBy, report.ReportReference);

        return report.Id;
    }
}