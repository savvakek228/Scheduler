using Microsoft.EntityFrameworkCore;
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
        modelBuilder.Entity<ScheduledTask>(b =>
        {
            b.HasKey(x => x.Id);
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
