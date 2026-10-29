using Microsoft.Extensions.Configuration;

namespace GymOS.Infrastructure.Services;

/// <summary>Converts gym-local calendar boundaries to UTC while keeping persisted instants in UTC.</summary>
public sealed class GymCalendar
{
    private readonly TimeProvider _timeProvider;

    public GymCalendar(IConfiguration configuration, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var timeZoneId = configuration["Gym:TimeZone"] ?? "America/Guayaquil";
        try
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (timeZoneId == "America/Guayaquil")
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        }
    }

    public TimeZoneInfo TimeZone { get; }
    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), TimeZone).DateTime);

    public DateTime StartOfDayUtc(DateOnly localDate)
    {
        var localMidnight = DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, TimeZone);
    }

    public (DateTime StartUtc, DateTime EndUtc) DayRangeUtc(DateOnly localDate) =>
        (StartOfDayUtc(localDate), StartOfDayUtc(localDate.AddDays(1)));

    public DateOnly ToLocalDate(DateTime utcDateTime) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), TimeZone));
}
