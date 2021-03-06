﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.SQLite.Utilities;
using Microsoft.Data.SQLite;

namespace Microsoft.Data.Entity.SQLite
{
    public class SQLiteDataStoreCreator : RelationalDataStoreCreator
    {
        private const int SQLITE_CANTOPEN = 14;

        private readonly SQLiteConnection _connection;
        private readonly SqlStatementExecutor _executor;
        private readonly SQLiteMigrationOperationSqlGenerator _generator;
        private readonly ModelDiffer _modelDiffer;

        public SQLiteDataStoreCreator(
            [NotNull] SQLiteConnection connection,
            [NotNull] SqlStatementExecutor executor,
            [NotNull] SQLiteMigrationOperationSqlGenerator generator,
            [NotNull] ModelDiffer modelDiffer)
        {
            Check.NotNull(connection, "connection");
            Check.NotNull(executor, "executor");
            Check.NotNull(generator, "generator");
            Check.NotNull(modelDiffer, "modelDiffer");

            _connection = connection;
            _executor = executor;
            _generator = generator;
            _modelDiffer = modelDiffer;
        }

        public override void Create()
        {
            using (var connection = _connection.CreateConnectionReadWriteCreate())
            {
                connection.Open();
            }
        }

        public override Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Create();

            return Task.FromResult(0);
        }

        public override void CreateTables(IModel model)
        {
            Check.NotNull(model, "model");

            // TODO: SQLiteMigrationOperationSqlGenerator should get this from DI
            _generator.Database = _modelDiffer.DatabaseBuilder.GetDatabase(model);
            var operations = _modelDiffer.CreateSchema(model);
            var statements = _generator.Generate(operations);

            // TODO: Delete database on error
            using (var connection = _connection.CreateConnectionReadWrite())
            {
                _executor.ExecuteNonQuery(connection, null, statements);
            }
        }

        public override Task CreateTablesAsync(IModel model, CancellationToken cancellationToken = default(CancellationToken))
        {
            CreateTables(model);

            return Task.FromResult(0);
        }

        public override bool Exists()
        {
            try
            {
                using (var connection = _connection.CreateConnectionReadOnly())
                {
                    connection.Open();
                    connection.Close();
                }

                return true;
            }
            catch (SQLiteException ex)
            {
                if (ex.ErrorCode != SQLITE_CANTOPEN)
                {
                    throw;
                }
            }

            return false;
        }

        public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(Exists());
        }

        public override bool HasTables()
        {
            return (long)_executor.ExecuteScalar(_connection.DbConnection, _connection.DbTransaction, CreateHasTablesCommand()) != 0;
        }

        public override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return (long)(await _executor.ExecuteScalarAsync(_connection.DbConnection, _connection.DbTransaction, CreateHasTablesCommand(), cancellationToken)) != 0;
        }

        private SqlStatement CreateHasTablesCommand()
        {
            return new SqlStatement("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND rootpage IS NOT NULL");
        }

        public override void Delete()
        {
            string filename = null;
            using (var connection = _connection.CreateConnectionReadOnly())
            {
                connection.Open();
                filename = connection.DataSource;
            }

            if (filename != null)
            {
                SQLiteEngine.DeleteDatabase(filename);
            }
        }

        public override Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Delete();

            return Task.FromResult(0);
        }
    }
}
