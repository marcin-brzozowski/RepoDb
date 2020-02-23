﻿using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepoDb.Extensions;
using RepoDb.IntegrationTests.Setup;
using RepoDb.SqlServer.BulkOperations.IntegrationTests.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace RepoDb.SqlServer.BulkOperations.IntegrationTests.Operations
{
    [TestClass]
    public class SqlConnectionBulkMergeOperationsTest
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

        #region BulkMerge<TEntity>

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntities()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10).AsList();

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                connection.BulkInsert(tables);

                // Setup
                tables.ForEach(t =>
                {
                    t.ColumnBit = !t.ColumnBit;
                    t.ColumnDateTime = DateTime.UtcNow.Date;
                    t.ColumnDateTime2 = DateTime.UtcNow;
                    t.ColumnDecimal = Convert.ToDecimal(new Random().Next());
                    t.ColumnFloat = Convert.ToSingle(new Random().Next());
                    t.ColumnInt = new Random().Next();
                    t.ColumnNVarChar = $"{t.ColumnNVarChar}-UPDATED";
                });

                // Act
                var bulkMergeResult = connection.BulkMerge(tables);

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMerge(tables);

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForEntitiesIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                connection.BulkMerge(tables, null, mappings);
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesDbDataReader()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMerge<BulkInsertIdentityTable>((DbDataReader)reader);

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesDbDataReaderWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMerge<BulkInsertIdentityTable>((DbDataReader)reader, null, mappings);

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForEntitiesDbDataReaderIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        destinationConnection.BulkMerge<BulkInsertIdentityTable>((DbDataReader)reader, null, mappings);
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesDataTable()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMerge<BulkInsertIdentityTable>(table);

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesDataTableWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMerge<BulkInsertIdentityTable>(table, null, DataRowState.Unchanged, mappings);

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count, queryResult.Count());
                            tables.AsList().ForEach(t =>
                            {
                                Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                            });
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForEntitiesDataTableIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            destinationConnection.BulkMerge<BulkInsertIdentityTable>(table, null, DataRowState.Unchanged, mappings);
                        }
                    }
                }
            }
        }

        #endregion

        #region BulkMerge<TEntity>(Extra Fields)

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesWithExtraFields()
        {
            // Setup
            var tables = Helper.CreateWithExtraFieldsBulkInsertIdentityTables(10);

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMerge(tables);

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForEntitiesWithExtraFieldsWithMappings()
        {
            // Setup
            var tables = Helper.CreateWithExtraFieldsBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMerge(tables);

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        #endregion

        #region BulkMerge(TableName)

        [TestMethod]
        public void TestSqlConnectionBulkMergeForTableNameDataEntities()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), tables);

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForTableNameDbDataReader()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), (DbDataReader)reader);
                        
                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForTableNameDbDataReaderWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                            (DbDataReader)reader,
                            null,
                            mappings);

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataReaderIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                            (DbDataReader)reader,
                            null,
                            mappings);

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataReaderIfTheTableNameIsNotValid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        destinationConnection.BulkMerge("InvalidTable", (DbDataReader)reader);
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataReaderIfTheTableNameIsMissing()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        destinationConnection.BulkMerge("MissingTable", (DbDataReader)reader);
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForTableNameDbDataTable()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), table);

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeForTableNameDbDataTableWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                                table,
                                null,
                                DataRowState.Unchanged,
                                mappings);

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataTableIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMerge(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                                table,
                                null,
                                DataRowState.Unchanged,
                                mappings);

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataTableIfTheTableNameIsNotValid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            destinationConnection.BulkMerge("InvalidTable",
                                table,
                                null,
                                DataRowState.Unchanged);
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeForTableNameDbDataTableIfTheTableNameIsMissing()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            destinationConnection.BulkMerge("MissingTable",
                                table,
                                null,
                                DataRowState.Unchanged);
                        }
                    }
                }
            }
        }

        #endregion

        #region BulkMergeAsync<TEntity>

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntities()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(tables).Result;

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(tables).Result;

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForEntitiesIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(tables,
                    null,
                    mappings);

                // Trigger
                var result = bulkMergeResult.Result;
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesDbDataReader()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>((DbDataReader)reader).Result;

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesDbDataReaderWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>((DbDataReader)reader,
                            null,
                            mappings).Result;

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForEntitiesDbDataReaderIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>((DbDataReader)reader,
                            null,
                            mappings);

                        // Trigger
                        var result = bulkMergeResult.Result;
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesDataTable()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>(table).Result;

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesDataTableWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>(table,
                                null,
                                DataRowState.Unchanged,
                                mappings).Result;

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForEntitiesDataTableIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync<BulkInsertIdentityTable>(table,
                                null,
                                DataRowState.Unchanged,
                                mappings);

                            // Trigger
                            var result = bulkMergeResult.Result;
                        }
                    }
                }
            }
        }

        #endregion

        #region BulkMergeAsync<TEntity>(Extra Fields)

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesWithExtraFields()
        {
            // Setup
            var tables = Helper.CreateWithExtraFieldsBulkInsertIdentityTables(10);

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(tables).Result;

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForEntitiesWithExtraFieldsWithMappings()
        {
            // Setup
            var tables = Helper.CreateWithExtraFieldsBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(tables).Result;

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        #endregion

        #region BulkMergeAsync(TableName)

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForTableNameDataEntities()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Act
                var bulkMergeResult = connection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), tables).Result;

                // Assert
                Assert.AreEqual(tables.Count, bulkMergeResult);

                // Act
                var queryResult = connection.QueryAll<BulkInsertIdentityTable>();

                // Assert
                Assert.AreEqual(tables.Count, queryResult.Count());
                tables.AsList().ForEach(t =>
                {
                    Helper.AssertPropertiesEquality(t, queryResult.ElementAt(tables.IndexOf(t)));
                });
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForTableNameDbDataReader()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), (DbDataReader)reader).Result;

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForTableNameDbDataReaderWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                            (DbDataReader)reader,
                            null,
                            mappings).Result;

                        // Assert
                        Assert.AreEqual(tables.Count, bulkMergeResult);

                        // Act
                        var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                        // Assert
                        Assert.AreEqual(tables.Count * 2, queryResult.Count());
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDbDataReaderIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                            (DbDataReader)reader,
                            null,
                            mappings);

                        // Trigger
                        var result = bulkMergeResult.Result;
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDbDataReaderIfTheTableNameIsNotValid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync("InvalidTable", (DbDataReader)reader);

                        // Trigger
                        var result = bulkMergeResult.Result;
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDbDataReaderIfTheTableNameIsMissing()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    // Open the destination connection
                    using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                    {
                        // Act
                        var bulkMergeResult = destinationConnection.BulkMergeAsync("MissingTable", (DbDataReader)reader);

                        // Trigger
                        var result = bulkMergeResult.Result;
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForTableNameDataTable()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(), table).Result;

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestSqlConnectionBulkMergeAsyncForTableNameDataTableWithMappings()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add the mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.RowGuid), nameof(BulkInsertIdentityTable.RowGuid)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnInt)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnNVarChar)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                                table,
                                null,
                                DataRowState.Unchanged,
                                mappings).Result;

                            // Assert
                            Assert.AreEqual(tables.Count, bulkMergeResult);

                            // Act
                            var queryResult = destinationConnection.QueryAll<BulkInsertIdentityTable>();

                            // Assert
                            Assert.AreEqual(tables.Count * 2, queryResult.Count());
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDataTableIfTheMappingsAreInvalid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);
            var mappings = new List<BulkInsertMapItem>();

            // Add invalid mappings
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnBit), nameof(BulkInsertIdentityTable.ColumnBit)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime), nameof(BulkInsertIdentityTable.ColumnDateTime)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDateTime2), nameof(BulkInsertIdentityTable.ColumnDateTime2)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnDecimal), nameof(BulkInsertIdentityTable.ColumnDecimal)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnFloat), nameof(BulkInsertIdentityTable.ColumnFloat)));

            // Switched
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnInt), nameof(BulkInsertIdentityTable.ColumnNVarChar)));
            mappings.Add(new BulkInsertMapItem(nameof(BulkInsertIdentityTable.ColumnNVarChar), nameof(BulkInsertIdentityTable.ColumnInt)));

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync(ClassMappedNameCache.Get<BulkInsertIdentityTable>(),
                                table,
                                null,
                                DataRowState.Unchanged,
                                mappings);

                            // Trigger
                            var result = bulkMergeResult.Result;
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDataTableIfTheTableNameIsNotValid()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync("InvalidTable", table);

                            // Trigger
                            var result = bulkMergeResult.Result;
                        }
                    }
                }
            }
        }

        [TestMethod, ExpectedException(typeof(AggregateException))]
        public void ThrowExceptionOnSqlConnectionBulkMergeAsyncForTableNameDataTableIfTheTableNameIsMissing()
        {
            // Setup
            var tables = Helper.CreateBulkInsertIdentityTables(10);

            // Insert the records first
            using (var connection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                connection.InsertAll(tables);
            }

            // Open the source connection
            using (var sourceConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
            {
                // Read the data from source connection
                using (var reader = sourceConnection.ExecuteReader("SELECT * FROM [dbo].[BulkInsertIdentityTable];"))
                {
                    using (var table = new DataTable())
                    {
                        table.Load(reader);

                        // Open the destination connection
                        using (var destinationConnection = new SqlConnection(Database.ConnectionStringForRepoDb))
                        {
                            // Act
                            var bulkMergeResult = destinationConnection.BulkMergeAsync("MissingTable",
                                table,
                                null,
                                DataRowState.Unchanged);

                            // Trigger
                            var result = bulkMergeResult.Result;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
