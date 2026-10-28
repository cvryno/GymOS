using GymOS.Domain.Entities;

namespace GymOS.Application.Interfaces;

public interface IWhatsAppService
{
    Task<bool> SendMembershipExpiryNotificationAsync(Member member, Membership membership);
    Task<bool> SendCustomMessageAsync(string phoneNumber, string message);
}
