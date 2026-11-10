using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Taurus.Core.Auditing;
using Taurus.Core.Users.Attributes;
using Taurus.Infrastructure.Configuration;
using Taurus.Infrastructure.Configuration.Attributes;
using Taurus.Infrastructure.Configuration.Tenants;
using Taurus.Infrastructure.Persistence;
using Taurus.Infrastructure.Persistence.Schemas;

namespace Taurus.Infrastructure.Schemas;

public sealed class SchemaManager(
    IdentityDatabase db,
    Settings settings,
    IUserLogRepo userLogRepo)
    : ISchemaService
{
    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        foreach (var tenant in settings.Tenants)
        {
            var snapshot = await db.Schemas.SingleOrDefaultAsync(
                x => x.TenantId == tenant.Id, cancellationToken);

            if (snapshot is null)
                throw new InvalidOperationException("Run bootstrap before serving");

            var definition = Settings.Schema(tenant);

            if (snapshot.Definition == definition)
                continue;

            if (Incompatible(snapshot.Definition, tenant).Count != default)
                throw new InvalidOperationException(
                    "Incompatible attribute schema; run schema-plan and schema-approve");

            snapshot.Definition = definition;
            snapshot.Version = Guid.NewGuid();
            userLogRepo.Add(new UserLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Action = AuditAction.SchemaApply,
                Severity = UserLogSeverity.High,
                Succeeded = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (!await userLogRepo.SaveAsync(AuditFailureMode.Block, cancellationToken))
            throw new InvalidOperationException("Required user audit could not be saved");
    }

    public async Task<string> PlanAsync(CancellationToken cancellationToken)
    {
        var text = new StringBuilder();

        foreach (var tenant in settings.Tenants)
        {
            var snapshot = await db.Schemas.AsNoTracking().SingleOrDefaultAsync(
                x => x.TenantId == tenant.Id, cancellationToken);
            var affected = Incompatible(snapshot?.Definition, tenant);

            text.Append(tenant.Id).Append(snapshot?.Version)
                .Append(Settings.Schema(tenant));

            foreach (var name in affected)
            {
                var count = await db.Attributes.CountAsync(
                    x => x.TenantId == tenant.Id && x.Name == name,
                    cancellationToken);

                Console.WriteLine($"Tenant {tenant.Id}: erase {name}: {count} values");
            }
        }

        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(text.ToString())));
    }

    public async Task ApproveAsync(
        string fingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var current = await PlanAsync(cancellationToken);

        if (!string.Equals(current, fingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Schema approval fingerprint changed");

        foreach (var tenant in settings.Tenants)
        {
            var snapshot = await db.Schemas.SingleOrDefaultAsync(
                x => x.TenantId == tenant.Id, cancellationToken);
            var affected = Incompatible(snapshot?.Definition, tenant);

            foreach (var name in affected)
            {
                await db.Attributes.Where(x => x.TenantId == tenant.Id && x.Name == name)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (snapshot is null)
            {
                snapshot = new SchemaSnapshot { TenantId = tenant.Id };
                db.Schemas.Add(snapshot);
            }

            snapshot.Definition = Settings.Schema(tenant);
            snapshot.Version = Guid.NewGuid();
            userLogRepo.Add(new UserLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Action = AuditAction.SchemaApproval,
                Severity = UserLogSeverity.High,
                Succeeded = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        if (!await userLogRepo.SaveAsync(AuditFailureMode.Block, cancellationToken))
            throw new InvalidOperationException("Required user audit could not be saved");

        await transaction.CommitAsync(cancellationToken);
    }

    private static HashSet<string> Incompatible(string? previous, Tenant tenant)
    {
        var affected = new HashSet<string>(StringComparer.Ordinal);

        if (previous is null)
            return affected;

        var old = JsonSerializer.Deserialize<List<AttributeDefinition>>(previous)!;

        foreach (var attribute in old)
        {
            AttributeDefinition? replacement = null;

            foreach (var candidate in tenant.Attributes)
            {
                if (candidate.Name == attribute.Name)
                {
                    replacement = candidate;
                    break;
                }
            }

            if (replacement is null || replacement.Kind != attribute.Kind
                || (!replacement.Multiple && attribute.Multiple)
                || replacement.MaximumLength < attribute.MaximumLength)
                affected.Add(attribute.Name);
        }

        foreach (var attribute in tenant.Attributes)
        {
            if (!attribute.Required)
                continue;

            var wasRequired = false;

            foreach (var prior in old)
            {
                if (prior.Name == attribute.Name && prior.Required)
                    wasRequired = true;
            }

            if (!wasRequired)
                throw new InvalidOperationException(
                    "New required attributes need an explicit data migration");
        }

        return affected;
    }
}
