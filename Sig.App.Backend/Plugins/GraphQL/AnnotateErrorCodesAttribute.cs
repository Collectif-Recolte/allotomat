using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Types.Descriptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sig.App.Backend.Plugins.MediatR;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class AnnotateErrorCodesAttribute : MetaDataAttributeBase
    {
        private readonly Type errorContainerType;

        public AnnotateErrorCodesAttribute(Type errorContainerType)
        {
            this.errorContainerType = errorContainerType;
        }

        public override void DeriveMetaData(GraphEntityInfo entity)
        {
            base.DeriveMetaData(entity);

            var errorCodesDescription = GetErrorCodesDescription();
            if (string.IsNullOrWhiteSpace(errorCodesDescription)) return;

            if (string.IsNullOrWhiteSpace(entity.Description))
                entity.Description = "";
            else
                entity.Description += "\n\n";

            entity.Description += errorCodesDescription;
        }

        private string GetErrorCodesDescription()
        {
            var errorTypes = GetValidationExceptionTypes().ToList();
            if (!errorTypes.Any()) return null;

            var sb = new StringBuilder();

            sb.AppendLine("**Expected error codes**");

            foreach (var errorType in errorTypes)
            {
                sb.AppendLine($"- {GetErrorCode(errorType)}");
            }

            return sb.ToString();
        }

        private IEnumerable<Type> GetValidationExceptionTypes()
        {
            return errorContainerType.GetNestedTypes()
                .Where(t => typeof(RequestValidationException).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract);
        }

        private string GetErrorCode(Type errorType) => NormalizeErrorCode(errorType);


        // Following code copied from GraphQL dotnet ExecutionError
        private static string NormalizeErrorCode(Type exceptionType)
        {
            string str = exceptionType.Name ?? string.Empty;
            if (str.EndsWith("Exception"))
                str = str.Substring(0, str.Length - "Exception".Length);
            if (str.StartsWith("GraphQL"))
                str = str.Substring("GraphQL".Length);
            return GetAllCapsRepresentation(str);
        }

        private static string GetAllCapsRepresentation(string str)
        {
            return Regex.Replace(NormalizeString(str), "([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", "$1$3_$2$4")
                .ToUpperInvariant();
        }

        private static string NormalizeString(string str)
        {
            str = str?.Trim();
            if (!string.IsNullOrWhiteSpace(str))
                return NormalizeTypeName(str);
            return string.Empty;
        }

        private static string NormalizeTypeName(string name)
        {
            int length = name.IndexOf('`');
            if (length < 0)
                return name;
            return name.Substring(0, length);
        }
    }
}