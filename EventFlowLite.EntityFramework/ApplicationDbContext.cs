using System.Diagnostics;
using EventFlowLite.Abstractions.Event;
using EventFlowLite.EntityFramework.Repository.ConversionHelpers;
using EventFlowLite.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EventFlowLite.EntityFramework;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Debugger.IsAttached)
            optionsBuilder.LogTo(Console.WriteLine);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainEventEntity>(entity =>
        {
            entity.ToTable("_DomainEvent");
            entity.HasIndex(e => e.AggregateName);
            entity.HasIndex(e => e.AggregateId);
            entity.HasIndex(e => e.AggregateVersion);
            entity.HasIndex(e => e.CommandName);

            // no json type in sql server :(
            // entity.Property(e => e.EventData).HasColumnType("json");

            entity.OwnsOne(e => e.CommandParams, commandParamsBuilder =>
            {
                NavigationBuilderHelpers.BuildPascalCaseNameNavigation(commandParamsBuilder, "CommandParams");

                commandParamsBuilder.HasIndex(c => c.CommandId);
                commandParamsBuilder.HasIndex(c => c.CorrelationId);

                commandParamsBuilder.Ignore(c => c.ExpectedVersion);

                commandParamsBuilder.Property(c => c.Metadata)
                    .HasJsonConversion();
            });
        });

        
        base.OnModelCreating(modelBuilder);
    }
}