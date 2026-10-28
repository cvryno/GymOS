using GymOS.Application.Interfaces;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace GymOS.Infrastructure.BackgroundJobs;

/// <summary>
/// Quartz.NET Job that runs daily at 9:00 AM to check memberships expiring in 3 days
/// and sends WhatsApp notifications to those members.
/// </summary>
[DisallowConcurrentExecution]
public class MembershipExpiryNotificationJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MembershipExpiryNotificationJob> _logger;

    public static readonly JobKey Key = new("membership-expiry-notification", "gym-jobs");

    public MembershipExpiryNotificationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<MembershipExpiryNotificationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("[MembershipExpiryJob] Starting at {Time}", DateTime.Now);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GymDbContext>();
        var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

        try
        {
            var today = DateTime.UtcNow.Date;
            var targetDate = today.AddDays(3);

            // Find memberships expiring in exactly 3 days that haven't been notified yet
            var expiringMemberships = await db.Memberships
                .Include(m => m.Member)
                .Include(m => m.Plan)
                .Where(m =>
                    m.Status == MembershipStatus.Active &&
                    m.EndDate.Date == targetDate &&
                    !m.NotificationSent &&
                    !m.Member.IsDeleted &&
                    !string.IsNullOrEmpty(m.Member.PhoneNumber))
                .ToListAsync(context.CancellationToken);

            _logger.LogInformation("[MembershipExpiryJob] Found {Count} memberships expiring on {Date}",
                expiringMemberships.Count, targetDate.ToString("yyyy-MM-dd"));

            int successCount = 0;
            int failCount = 0;

            foreach (var membership in expiringMemberships)
            {
                try
                {
                    var success = await whatsAppService.SendMembershipExpiryNotificationAsync(
                        membership.Member, membership);

                    if (success)
                    {
                        membership.NotificationSent = true;
                        membership.NotificationSentAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(context.CancellationToken);
                        successCount++;
                        _logger.LogInformation("[MembershipExpiryJob] Notification sent to {Name} ({Phone})",
                            membership.Member.FullName, membership.Member.PhoneNumber);
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning("[MembershipExpiryJob] Failed to notify {Name}",
                            membership.Member.FullName);
                    }

                    // Rate limiting: wait 500ms between messages to avoid API limits
                    await Task.Delay(500, context.CancellationToken);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "[MembershipExpiryJob] Error processing membership {Id}", membership.Id);
                }
            }

            _logger.LogInformation(
                "[MembershipExpiryJob] Completed. Sent: {Success}, Failed: {Fail}, Total: {Total}",
                successCount, failCount, expiringMemberships.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MembershipExpiryJob] Unexpected error");
            throw new JobExecutionException(ex, false);
        }
    }
}
