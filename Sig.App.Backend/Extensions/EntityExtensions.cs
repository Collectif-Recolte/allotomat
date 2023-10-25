using GraphQL.Conventions;
using System;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Extensions
{
    public static class EntityExtensions
    {
        public static Id GetIdentifier<TEntity>(this TEntity entity) where TEntity : IHaveIdentifier
        {
            if (entity is IHaveLongIdentifier li) return Id.New<TEntity>(li.Id);
            if (entity is IHaveStringIdentifier si) return Id.New<TEntity>(si.Id);

            throw new ArgumentException("Unkown identifier type");
        }
    }
}