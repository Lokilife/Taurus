using MediatR;
using Taurus.Core.Provisioning;

namespace Taurus.Application.Features.Provisioning.Bootstrap;

public sealed class BootstrapHandler(IBootstrapService service)
    : IRequestHandler<BootstrapCommand>
{
    public Task Handle(BootstrapCommand request, CancellationToken cancellationToken) =>
        service.RunAsync(cancellationToken);
}
