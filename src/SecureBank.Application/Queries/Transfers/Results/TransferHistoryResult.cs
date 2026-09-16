using SecureBank.Application.Abstractions.Models;

namespace SecureBank.Application.Queries.Transfers.Results;

public sealed class TransferHistoryResult(IReadOnlyCollection<TransferResult> items);
