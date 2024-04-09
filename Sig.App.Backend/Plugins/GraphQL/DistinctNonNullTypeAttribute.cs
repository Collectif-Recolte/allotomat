using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Types.Descriptors;
using GraphQL.Conventions;
using System;
using System.Reflection;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class DistinctNonNullTypeAttribute : MetaDataAttributeBase
    {
        public override void MapType(GraphTypeInfo type, TypeInfo typeInfo)
        {
            base.MapType(type, typeInfo);

            if (typeInfo.IsGenericType)
                typeInfo = typeInfo.GetGenericArguments()[0].GetTypeInfo();

            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(NonNull<>))
                type.Name += "NonNull";
            else if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
                type.Name += "Nullable";
        }
    }
}
