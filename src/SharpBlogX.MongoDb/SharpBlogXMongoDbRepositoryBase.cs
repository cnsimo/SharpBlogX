﻿using MongoDB.Bson;
using MongoDB.Driver;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.MongoDB;
using Volo.Abp.MongoDB;

namespace SharpBlogX
{
    public class MongoDbRepositoryBase<TMongoDbContext, TEntity, TKey> : MongoDbRepository<TMongoDbContext, TEntity, TKey> where TMongoDbContext : IAbpMongoDbContext where TEntity : class, IEntity<TKey>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<TMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public new IMongoCollection<TEntity> Collection => GetCollectionAsync().Result;
    }

    public class MongoDbRepositoryBase<TEntity, TKey> : MongoDbRepositoryBase<SharpBlogXMongoDbContext, TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<SharpBlogXMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }

    public class MongoDbRepositoryBase<TEntity> : MongoDbRepositoryBase<TEntity, ObjectId> where TEntity : class, IEntity<ObjectId>
    {
        public MongoDbRepositoryBase(IMongoDbContextProvider<SharpBlogXMongoDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}