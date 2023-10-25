using System.Globalization;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Razor
{
    public interface IRazorRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model, CultureInfo culture);
    }
}
