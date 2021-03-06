﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity.Relational.Query
{
    public class AsyncQueryMethodProvider : IQueryMethodProvider
    {
        public virtual MethodInfo QueryMethod
        {
            get { return _queryMethodInfo; }
        }

        private static readonly MethodInfo _queryMethodInfo
            = typeof(AsyncQueryMethodProvider).GetTypeInfo()
                .GetDeclaredMethod("_Query");

        [UsedImplicitly]
        private static IAsyncEnumerable<T> _Query<T>(
            QueryContext queryContext, CommandBuilder commandBuilder, Func<DbDataReader, T> shaper)
        {
            return new AsyncEnumerable<T>(
                ((RelationalQueryContext)queryContext).Connection,
                commandBuilder,
                shaper,
                queryContext.Logger);
        }

        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly RelationalConnection _connection;
            private readonly CommandBuilder _commandBuilder;
            private readonly Func<DbDataReader, T> _shaper;
            private readonly ILogger _logger;

            public AsyncEnumerable(
                RelationalConnection connection,
                CommandBuilder commandBuilder,
                Func<DbDataReader, T> shaper,
                ILogger logger)
            {
                _connection = connection;
                _commandBuilder = commandBuilder;
                _shaper = shaper;
                _logger = logger;
            }

            public IAsyncEnumerator<T> GetEnumerator()
            {
                return new AsyncEnumerator(this);
            }

            private sealed class AsyncEnumerator : IAsyncEnumerator<T>
            {
                private readonly AsyncEnumerable<T> _enumerable;

                private DbCommand _command;
                private DbDataReader _reader;

                public AsyncEnumerator(AsyncEnumerable<T> enumerable)
                {
                    _enumerable = enumerable;
                }

                public async Task<bool> MoveNext(CancellationToken cancellationToken)
                {
                    var hasNext
                        = await (_reader == null
                            ? InitializeAndReadAsync(cancellationToken)
                            : _reader.ReadAsync(cancellationToken));

                    if (!hasNext)
                    {
                        // H.A.C.K.: Workaround https://github.com/Reactive-Extensions/Rx.NET/issues/5
                        Dispose();
                    }

                    return hasNext;
                }

                private async Task<bool> InitializeAndReadAsync(
                    CancellationToken cancellationToken = default(CancellationToken))
                {
                    await _enumerable._connection.OpenAsync(cancellationToken);

                    _command = _enumerable._commandBuilder.Build(_enumerable._connection);

                    _enumerable._logger.WriteSql(_command.CommandText);

                    _reader = await _command.ExecuteReaderAsync(cancellationToken);

                    return await _reader.ReadAsync(cancellationToken);
                }

                public T Current
                {
                    get
                    {
                        if (_reader == null)
                        {
                            return default(T);
                        }

                        return _enumerable._shaper(_reader);
                    }
                }

                public void Dispose()
                {
                    if (_reader != null)
                    {
                        _reader.Dispose();
                    }

                    if (_command != null)
                    {
                        _command.Dispose();
                    }

                    if (_enumerable._connection != null)
                    {
                        _enumerable._connection.Close();
                    }
                }
            }
        }
    }
}
