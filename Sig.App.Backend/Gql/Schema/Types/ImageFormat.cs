using System.Collections.Generic;
using System.Linq;
using GraphQL.Conventions;
using SixLabors.ImageSharp.Processing;

namespace Sig.App.Backend.Gql.Schema.Types
{
    [InputType]
    public class ImageFormat
    {
        public int? Width { get; set; }
        public int? Height { get; set; }
        public ResizeMode? ResizeMode { get; set; }
        public OutputFormat? Format { get; set; }
        public string BackgroundColor { get; set; }

        public string GetQueryString()
        {
            var parameters = new List<string>();

            if (Width.HasValue)
                parameters.Add($"width={Width}");
            if (Height.HasValue)
                parameters.Add($"height={Height}");
            if (ResizeMode.HasValue)
                parameters.Add($"rmode={ResizeMode}");
            if (Format.HasValue)
                parameters.Add($"format={Format}");
            if (!string.IsNullOrWhiteSpace(BackgroundColor))
                parameters.Add($"bgcolor={BackgroundColor}");

            return parameters.Any() 
                ? $"?{string.Join('&', parameters)}" 
                : null;
        }

        public enum OutputFormat { Jpg, Png }
    }
}