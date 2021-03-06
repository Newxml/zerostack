﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ZeroStack.DeviceCenter.Domain.Aggregates.TenantAggregate;
using ZeroStack.DeviceCenter.Domain.Entities;
using ZeroStack.DeviceCenter.Domain.UnitOfWork;

namespace ZeroStack.DeviceCenter.Infrastructure.EntityFrameworks
{
    public class DeviceCenterDbContext : DbContext, IUnitOfWork
    {
        private readonly IMediator _mediator;

        public DeviceCenterDbContext(DbContextOptions<DeviceCenterDbContext> options) : base(options) => _mediator = this.GetInfrastructure().GetService<IMediator>() ?? new NullMediator();

        async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => await base.SaveChangesAsync(cancellationToken);

        public async override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var deletedEntries = ChangeTracker.Entries().Where(entry => entry.State == EntityState.Deleted && entry.Entity is ISoftDelete);

            deletedEntries?.ToList().ForEach(entityEntry =>
            {
                entityEntry.Reload();
                entityEntry.State = EntityState.Modified;
                ((ISoftDelete)entityEntry.Entity).IsDeleted = true;
            });

            await DispatchDomainEventsAsync(cancellationToken);

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
        {
            var domainEntities = ChangeTracker.Entries<BaseEntity>().OfType<IDomainEvents>();
            var domainEvents = domainEntities.SelectMany(x => x.DomainEvents).ToList();
            domainEntities.ToList().ForEach(entity => entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.IsAssignableTo(typeof(IMultiTenant)))
                {
                    ICurrentTenant? currentTenant = this.GetInfrastructure().GetService<ICurrentTenant>();

                    if (currentTenant?.Id is not null)
                    {
                        modelBuilder.Entity(entityType.ClrType).AddQueryFilter<IMultiTenant>(e => e.TenantId == currentTenant.Id);
                    }
                }

                if (entityType.ClrType.IsAssignableTo(typeof(ISoftDelete)))
                {
                    modelBuilder.Entity(entityType.ClrType).AddQueryFilter<ISoftDelete>(e => !e.IsDeleted);
                }
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
