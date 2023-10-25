using GraphQL.Conventions;
using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Types.Descriptors;
using System.Reflection;
using System.Threading.Tasks;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class MutationPayloadAttribute : MetaDataAttributeBase
    {
        public override void MapType(GraphTypeInfo type, TypeInfo typeInfo)
        {
            base.MapType(type, typeInfo);

            if (typeInfo.IsGenericType && (typeInfo.GetGenericTypeDefinition() == typeof(Task<>) || typeInfo.GetGenericTypeDefinition() == typeof(NonNull<>)))
                typeInfo = typeInfo.GetGenericArguments()[0].GetTypeInfo();

            if (typeInfo.DeclaringType != null)
                type.Name = $"{typeInfo.DeclaringType.Name}Payload";
        }
    }
}