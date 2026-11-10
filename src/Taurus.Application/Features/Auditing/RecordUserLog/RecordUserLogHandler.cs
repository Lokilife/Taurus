using MediatR;
using Taurus.Core.Auditing;

namespace Taurus.Application.Features.Auditing.RecordUserLog;

public sealed class RecordUserLogHandler(IUserLogRepo repo)
    : IRequestHandler<RecordUserLogCommand, bool>
{
    public Task<bool> Handle(
        RecordUserLogCommand request,
        CancellationToken cancellationToken)
    {
        repo.Add(request.Entry);

        return repo.SaveAsync(request.FailureMode, cancellationToken);
    }
}
