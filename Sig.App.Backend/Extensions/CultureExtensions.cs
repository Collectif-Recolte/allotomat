using System;
using System.Globalization;
using Sig.App.Backend.Utilities;

namespace Sig.App.Backend.Extensions
{
    public static class CultureExtensions
    {
        public static IDisposable Substitute(this CultureInfo culture) => new CultureSubstitution(culture);
    }
}
