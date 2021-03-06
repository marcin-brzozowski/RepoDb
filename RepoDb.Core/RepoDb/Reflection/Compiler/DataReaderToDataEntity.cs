﻿using System;
using System.Data.Common;
using System.Data;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using RepoDb.Interfaces;
using System.Threading.Tasks;

namespace RepoDb.Reflection
{
    internal partial class Compiler
    {
        /// <summary>
        /// Gets a compiled function that is used to convert the <see cref="DbDataReader"/> object into a list of data entity objects.
        /// </summary>
        /// <typeparam name="TEntity">The data entity object to convert to.</typeparam>
        /// <param name="reader">The <see cref="DbDataReader"/> to be converted.</param>
        /// <param name="connection">The used <see cref="IDbConnection"/> object.</param>
        /// <param name="connectionString">The raw connection string.</param>
        /// <param name="transaction">The transaction object that is currently in used.</param>
        /// <param name="enableValidation">Enables the validation after retrieving the database fields.</param>
        /// <returns>A compiled function that is used to cover the <see cref="DbDataReader"/> object into a list of data entity objects.</returns>
        public static Func<DbDataReader, TEntity> CompileDataReaderToDataEntity<TEntity>(DbDataReader reader,
            IDbConnection connection,
            string connectionString,
            IDbTransaction transaction,
            bool enableValidation)
            where TEntity : class
        {
            // Expression variables
            var dbFields = GetDbFields(connection,
                ClassMappedNameCache.Get<TEntity>(),
                connectionString,
                transaction,
                enableValidation);

            // return the value
            return CompileDataReaderToDataEntity<TEntity>(reader, dbFields, connection?.GetDbSetting());
        }

        /// <summary>
        /// Gets a compiled function that is used to convert the <see cref="DbDataReader"/> object into a list of data entity objects in an asynchronous way.
        /// </summary>
        /// <typeparam name="TEntity">The data entity object to convert to.</typeparam>
        /// <param name="reader">The <see cref="DbDataReader"/> to be converted.</param>
        /// <param name="connection">The used <see cref="IDbConnection"/> object.</param>
        /// <param name="connectionString">The raw connection string.</param>
        /// <param name="transaction">The transaction object that is currently in used.</param>
        /// <param name="enableValidation">Enables the validation after retrieving the database fields.</param>
        /// <returns>A compiled function that is used to cover the <see cref="DbDataReader"/> object into a list of data entity objects.</returns>
        public static async Task<Func<DbDataReader, TEntity>> CompileDataReaderToDataEntityAsync<TEntity>(DbDataReader reader,
            IDbConnection connection,
            string connectionString,
            IDbTransaction transaction,
            bool enableValidation)
            where TEntity : class
        {
            // Expression variables
            var dbFields = await GetDbFieldsAsync(connection,
                ClassMappedNameCache.Get<TEntity>(),
                connectionString,
                transaction,
                enableValidation);

            // return the value
            return CompileDataReaderToDataEntity<TEntity>(reader, dbFields, connection?.GetDbSetting());
        }

        /// <summary>
        /// Gets a compiled function that is used to convert the <see cref="DbDataReader"/> object into a list of data entity objects.
        /// </summary>
        /// <typeparam name="TEntity">The data entity object to convert to.</typeparam>
        /// <param name="reader">The <see cref="DbDataReader"/> to be converted.</param>
        /// <param name="dbFields">The list of the <see cref="DbField"/> objects.</param>
        /// <param name="dbSetting">The database setting that is being used.</param>
        /// <returns>A compiled function that is used to cover the <see cref="DbDataReader"/> object into a list of data entity objects.</returns>
        public static Func<DbDataReader, TEntity> CompileDataReaderToDataEntity<TEntity>(DbDataReader reader,
            IEnumerable<DbField> dbFields,
            IDbSetting dbSetting)
            where TEntity : class
        {
            var readerParameterExpression = Expression.Parameter(StaticType.DbDataReader, "reader");
            var readerFields = GetDataReaderFields(reader, dbFields, dbSetting);
            var memberBindings = GetMemberBindingsForDataEntity<TEntity>(readerParameterExpression,
                readerFields, dbSetting);
            var memberAssignments = memberBindings?
                .Where(item => item.MemberAssignment != null)
                .Select(item => item.MemberAssignment);
            var arguments = memberBindings?
                .Where(item => item.Argument != null)
                .Select(item => item.Argument);
            var typeOfEntity = typeof(TEntity);

            // Throw an error if there are no bindings
            if (arguments?.Any() != true && memberAssignments?.Any() != true)
            {
                throw new InvalidOperationException($"There are no 'contructor parameter' and/or 'property member' bindings found between the resultset of the data reader and the type '{typeOfEntity.FullName}'.");
            }

            // Initialize the members
            var constructorInfo = typeOfEntity
                .GetConstructors()?
                .Where(item => item.GetParameters().Length > 0)?
                .OrderByDescending(item => item.GetParameters().Length)?
                .FirstOrDefault();
            var entityExpression = (Expression)null;

            // Check the arguments
            entityExpression = arguments?.Any() == true ?
                Expression.New(constructorInfo, arguments) : Expression.New(typeOfEntity);

            // Bind the members
            entityExpression = memberAssignments?.Any() == true ?
                (Expression)Expression.MemberInit((NewExpression)entityExpression, memberAssignments) : entityExpression;

            // Class handler
            entityExpression = ConvertValueExpressionToClassHandlerGetExpression<TEntity>(entityExpression,
                readerParameterExpression);

            // Set the function value
            return Expression
                .Lambda<Func<DbDataReader, TEntity>>(entityExpression, readerParameterExpression)
                .Compile();
        }
    }
}
