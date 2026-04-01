using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Scheduler.Domain.DeadLetters;
using Scheduler.Domain.Inbox;
using Scheduler.Domain.Outbox;
using Scheduler.Domain.Tasks;

namespace Scheduler.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxRecord> InboxRecords => Set<InboxRecord>();
    public DbSet<DeadLetterItem> DeadLetterItems => Set<DeadLetterItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetTicks = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));

        var dateTimeOffsetTicksNullable = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.UtcTicks : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

        modelBuilder.Entity<ScheduledTask>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.CreatedAt).HasConversion(dateTimeOffsetTicks);
            b.Property(x => x.RunAt).HasConversion(dateTimeOffsetTicks);
            b.Property(x => x.NextAttemptAt).HasConversion(dateTimeOffsetTicksNullable);
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.HasIndex(x => x.MessageId).IsUnique();
        });

        modelBuilder.Entity<InboxRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.MessageId).IsUnique();
        });

        modelBuilder.Entity<DeadLetterItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
        });
    }
}
