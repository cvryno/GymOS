using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GymOS.Application.Interfaces;
using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymOS.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly GymDbContext _db;
    private readonly HttpClient _httpClient;

    private string PhoneNumberId => _config["WhatsApp:PhoneNumberId"] ?? throw new InvalidOperationException("WhatsApp:PhoneNumberId not configured");
    private string AccessToken => _config["WhatsApp:AccessToken"] ?? throw new InvalidOperationException("WhatsApp:AccessToken not configured");
    private string GymName => _config["WhatsApp:GymName"] ?? "GymOS";

    public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> logger,
        GymDbContext db, HttpClient httpClient)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _httpClient = httpClient;
    }

    public async Task<bool> SendMembershipExpiryNotificationAsync(Member member, Membership membership)
    {
        var phone = NormalizePhoneNumber(member.PhoneNumber);
        var daysRemaining = membership.DaysRemaining;
        var message = BuildExpiryMessage(member.FullName, membership.Plan.Name, daysRemaining, membership.EndDate);

        var log = new WhatsAppNotificationLog
        {
            MemberId = member.Id,
            MembershipId = membership.Id,
            PhoneNumber = phone,
            MessageBody = message,
            SentAt = DateTime.UtcNow
        };

        try
        {
            var result = await SendTextMessageAsync(phone, message);

            log.Status = result.success ? NotificationStatus.Sent : NotificationStatus.Failed;
            log.WhatsAppMessageId = result.messageId;
            log.ErrorMessage = result.error;

            // Mark membership as notified
            if (result.success)
            {
                membership.NotificationSent = true;
                membership.NotificationSentAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp notification to {Phone}", phone);
            log.Status = NotificationStatus.Failed;
            log.ErrorMessage = "No se pudo enviar el mensaje. Revisa la configuración y los logs del servidor.";
        }

        _db.WhatsAppNotificationLogs.Add(log);
        await _db.SaveChangesAsync();

        return log.Status == NotificationStatus.Sent;
    }

    public async Task<bool> SendCustomMessageAsync(string phoneNumber, string message)
    {
        var phone = NormalizePhoneNumber(phoneNumber);
        var result = await SendTextMessageAsync(phone, message);
        return result.success;
    }

    private async Task<(bool success, string? messageId, string? error)> SendTextMessageAsync(string phone, string message)
    {
        var url = $"https://graph.facebook.com/v21.0/{PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = phone,
            type = "text",
            text = new { preview_url = false, body = message }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var messageId = doc.RootElement
                .GetProperty("messages")[0]
                .GetProperty("id")
                .GetString();
            _logger.LogInformation("WhatsApp message sent successfully. MessageId: {MessageId}", messageId);
            return (true, messageId, null);
        }
        else
        {
            _logger.LogError("WhatsApp API error: {StatusCode} - {Body}", response.StatusCode, responseBody);
            return (false, null, $"{response.StatusCode}: {responseBody}");
        }
    }

    private string BuildExpiryMessage(string memberName, string planName, int daysRemaining, DateTime endDate)
    {
        var daysText = daysRemaining == 1 ? "1 día" : $"{daysRemaining} días";
        var dateFormatted = endDate.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));

        return $"""
        🏋️ *{GymName}* - Recordatorio de Membresía

        Hola *{memberName}*, 👋

        Te recordamos que tu membresía *{planName}* vence en *{daysText}* (el {dateFormatted}).

        ⏰ Para no perder acceso al gimnasio, renueva antes de la fecha de vencimiento.

        💳 Puedes renovar directamente en recepción o contactarnos.

        ¡Seguimos entrenando juntos! 💪
        """;
    }

    private static string NormalizePhoneNumber(string phone)
    {
        // Remove spaces, dashes, parentheses
        var cleaned = new string(phone.Where(char.IsDigit).ToArray());

        // Ensure it starts with country code (default to +57 Colombia if no code)
        if (cleaned.Length == 10 && !cleaned.StartsWith("57"))
            cleaned = "57" + cleaned;

        return cleaned;
    }
}
