using System;
using System.Globalization;
using GraphQL.NodaTime;
using GraphQL.Types;
using NodaTime;
using NodaTime.Text;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class RfcOffsetDateTimeGraphType : ScalarGraphType
    {
        public RfcOffsetDateTimeGraphType()
        {
            Name = "OffsetDateTime";
            Description = "A local date and time in a particular calendar system, combined with an offset from UTC.";
        }

        public override object Serialize(object value)
        {
            if (value is OffsetDateTime odt)
                return OffsetDateTimePattern.Rfc3339.WithCulture(CultureInfo.InvariantCulture).Format(odt);

            if (value is string)
            {
                return value;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return dateTimeOffset.ToString("o", CultureInfo.InvariantCulture);
            }

            if (value is OffsetDateTime value2)
            {
                return OffsetDateTimePattern.ExtendedIso.WithCulture(CultureInfo.InvariantCulture).Format(value2);
            }

            return null;
        }

        private static object FromString(string stringValue)
        {
            return ParserComposer.FirstNonThrowing(new Func<string, OffsetDateTime>[2]
            {
            (string str) => OffsetDateTimePattern.ExtendedIso.WithCulture(CultureInfo.InvariantCulture).Parse(str).GetValueOrThrow(),
            (string str) => OffsetDateTimePattern.CreateWithInvariantCulture("yyyy'-'MM'-'dd'T'HH':'mm':'sso<+HHmm>").Parse(str).GetValueOrThrow()
            }, stringValue);
        }

        private static object FromDateTimeUtc(DateTime dateTime)
        {
            try
            {
                return Instant.FromDateTimeUtc(dateTime);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override object ParseValue(object value)
        {
            if (value is string stringValue)
            {
                return FromString(stringValue);
            }

            if (value is DateTime dateTime)
            {
                return FromDateTimeUtc(dateTime);
            }

            return null;
        }
    }
}