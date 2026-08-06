using FinFlow.Orion.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinFlow.Orion.Infrastructure.Providers.Bank;

/// <summary>
/// Generic bank transfer provider.
/// Can be backed by Pesalink, RTGS, EFT, or any bank API.
/// </summary>
public sealed class BankProvider : IBankProvider
{
    private readonly ILogger<BankProvider> _logger;

    public BankProvider(ILogger<BankProvider> logger)
        => _logger = logger;

    public async Task<BankTransferResult> InitiateTransferAsync(
        BankTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Bank] Initiating transfer — Account: {Account} | Bank: {Bank} | Amount: {Amount} | Key: {Key}",
                request.AccountNumber, request.BankCode, request.Amount, request.IdempotencyKey);

            // TODO: Inject and call the specific bank API client here
            // e.g. Pesalink, RTGS gateway, CBK KEPSS

            await Task.Delay(0, cancellationToken);

            return new BankTransferResult
            {
                IsSuccessful = true,
                TransactionId = $"BANK-{Guid.NewGuid():N}",
                Status = "processing",
                BankReference = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Bank] Transfer failed — Account: {Account}", request.AccountNumber);

            return new BankTransferResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }

    public async Task<BankTransferResult> QueryTransferStatusAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Bank] Querying transfer — TxId: {Id}", providerTransactionId);

            await Task.Delay(0, cancellationToken);

            return new BankTransferResult
            {
                IsSuccessful = true,
                TransactionId = providerTransactionId,
                Status = "completed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Bank] Query failed — TxId: {Id}", providerTransactionId);

            return new BankTransferResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }

    public async Task<BankTransferResult> ReverseTransferAsync(
        string providerTransactionId,
        Money amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Bank] Reversing — TxId: {Id} | Amount: {Amount} | Reason: {Reason}",
                providerTransactionId, amount, reason);

            await Task.Delay(0, cancellationToken);

            return new BankTransferResult
            {
                IsSuccessful = true,
                TransactionId = providerTransactionId,
                Status = "reversed"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Bank] Reversal failed — TxId: {Id}", providerTransactionId);

            return new BankTransferResult
            {
                IsSuccessful = false,
                FailureMessage = ex.Message
            };
        }
    }
}