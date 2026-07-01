using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SESAggregator.Data.Entities;

public class Message : IEntityTypeConfiguration<Message>
{
    public int Id { get; set; }
    public required string ToAddress { get; set; }
    public required string ToName { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public required string SendingApplication { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? LastSendingAttemptAt { get; set; }
    public bool SuccessfullySent { get; set; }
    public bool PermanentlyFailed { get; set; }
    public int Tries { get; set; }
    public string? LastFailureMessage { get; set; }

    public void Configure(EntityTypeBuilder<Message> builder) { }
}
