using System;
using NodaTime;

namespace Sig.App.Backend.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime AsUtc(this DateTime dt) => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeKind.Local => throw new ArgumentException("Local date should not be used as UTC.", nameof(dt)),
            _ => throw new ArgumentOutOfRangeException(nameof(dt), dt.Kind, "Unkown DateTimeKind")
        };

        public static OffsetDateTime FromUtcToOffsetDateTime(this DateTime value)
        {
            return Instant
                .FromDateTimeUtc(value.AsUtc())
                .InUtc()
                .ToOffsetDateTime();
        }
    }
}