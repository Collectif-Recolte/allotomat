using System.Globalization;
using GraphQL.NodaTime;
using NodaTime;
using NodaTime.Text;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class RfcOffsetDateTimeGraphType : OffsetDateTimeGraphType
    {
        public override object Serialize(object value)
        {
            if (value is OffsetDateTime odt)
                return OffsetDateTimePattern.Rfc3339.WithCulture(CultureInfo.InvariantCulture).Format(odt);

            return base.Serialize(value);
        }
    }
}
