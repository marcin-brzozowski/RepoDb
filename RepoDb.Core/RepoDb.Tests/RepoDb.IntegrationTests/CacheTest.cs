﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepoDb.IntegrationTests.Models;
using RepoDb.IntegrationTests.Setup;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;

namespace RepoDb.IntegrationTests.Caches
{
    [TestClass]
    public class CacheTest
    {
        [TestInitialize]
        public void Initialize()
        {
            Database.Initialize();
            Cleanup();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Database.Cleanup();
        }

        #region Helper

        private IdentityTable GetIdentityTable()
        {
            var random = new Random();
            return new IdentityTable
            {
                RowGuid = Guid.NewGuid(),
                ColumnBit = true,
                ColumnDateTime = DateTime.UtcNow,
                ColumnDateTime2 = DateTime.UtcNow,
                ColumnDecimal = Convert.ToDecimal(random.Next(int.MinValue, int.MaxValue)),
                ColumnFloat = Convert.ToSingle(random.Next(int.MinValue, int.MaxValue)),
                ColumnInt = random.Next(int.MinValue, int.MaxValue),
                ColumnNVarChar = Guid.NewGuid().ToString()
            };
        }

        #endregion

        #region ExecuteQuery

        [TestMethod]
        public void TestSqlConnectionExecuteQueryCache()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.ExecuteQuery<IdentityTable>("SELECT * FROM [sc].[IdentityTable];",
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    cache: cache);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionExecuteQueryCacheViaDynamics()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.ExecuteQuery("SELECT * FROM [sc].[IdentityTable];",
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    cache: cache);
                var item = cache.Get<IEnumerable<dynamic>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion

        #region ExecuteQueryAsync

        [TestMethod]
        public void TestSqlConnectionExecuteQueryAsyncCache()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.ExecuteQueryAsync<IdentityTable>("SELECT * FROM [sc].[IdentityTable];",
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    cache: cache).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionExecuteQueryAsyncCacheViaDynamics()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.ExecuteQueryAsync("SELECT * FROM [sc].[IdentityTable];",
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    cache: cache).Result;
                var item = cache.Get<IEnumerable<dynamic>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion

        #region Query

        [TestMethod]
        public void TestSqlConnectionQueryCacheViaDynamics()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.Query<IdentityTable>(what: (object)null,
                    orderBy: (IEnumerable<OrderField>)null,
                    top: null,
                    hints: null,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: null,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: Helper.StatementBuilder);

                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryCacheViaQueryField()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.Query<IdentityTable>(where: (QueryGroup)null,
                    orderBy: null,
                    top: 0,
                    hints: null,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryCacheViaQueryFields()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.Query<IdentityTable>(where: (IEnumerable<QueryField>)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryCacheViaExpression()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.Query<IdentityTable>(where: (Expression<Func<IdentityTable, bool>>)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryCacheViaQueryGroup()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.Query<IdentityTable>(where: (QueryGroup)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion

        #region QueryAsync

        [TestMethod]
        public void TestSqlConnectionQueryAsyncCacheViaDynamics()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAsync<IdentityTable>(what: (object)null,
                    orderBy: (IEnumerable<OrderField>)null,
                    top: null,
                    hints: null,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: null,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: Helper.StatementBuilder).Result;

                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryAsyncCacheViaQueryField()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAsync<IdentityTable>(where: (QueryField)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryAsyncCacheViaQueryFields()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAsync<IdentityTable>(where: (IEnumerable<QueryField>)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryAsyncCacheViaExpression()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAsync<IdentityTable>(where: (Expression<Func<IdentityTable, bool>>)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        [TestMethod]
        public void TestSqlConnectionQueryAsyncCacheViaQueryGroup()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAsync<IdentityTable>(where: (QueryGroup)null,
                    orderBy: null,
                    top: 0,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion

        #region QueryAll

        [TestMethod]
        public void TestSqlConnectionQueryAllCache()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAll<IdentityTable>(orderBy: null,
                    hints: null,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null);
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion

        #region QueryAllAsync

        [TestMethod]
        public void TestSqlConnectionQueryAllAsyncCache()
        {
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Setup
                var cache = new MemoryCache();
                var entity = GetIdentityTable();
                var cacheKey = "SimpleTables";
                var cacheItemExpiration = 60;

                // Act
                entity.Id = Convert.ToInt32(connection.Insert(entity));

                // Act
                var result = connection.QueryAllAsync<IdentityTable>(orderBy: null,
                    hints: null,
                    cacheKey: cacheKey,
                    cacheItemExpiration: cacheItemExpiration,
                    commandTimeout: 0,
                    transaction: null,
                    cache: cache,
                    trace: null,
                    statementBuilder: null).Result;
                var item = cache.Get<IEnumerable<IdentityTable>>(cacheKey);

                // Assert
                Assert.AreEqual(1, result.Count());
                Assert.IsNotNull(item);
                Assert.AreEqual(cacheItemExpiration, (item.Expiration - item.CreatedDate).TotalMinutes);
            }
        }

        #endregion
    }
}
