using System;
using System.Globalization;

namespace Sig.App.Backend.Utilities
{
    public class CultureSubstitution : IDisposable
    {
        private readonly CultureInfo previousCulture;
        private readonly CultureInfo previousUiCulture;

        public CultureSubstitution(CultureInfo culture)
        {
            previousCulture = CultureInfo.CurrentCulture;
            previousUiCulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
