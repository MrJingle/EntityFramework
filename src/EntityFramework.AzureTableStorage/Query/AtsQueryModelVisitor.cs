﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.AzureTableStorage.Metadata;
using Microsoft.Data.Entity.AzureTableStorage.Query.Expressions;
using Microsoft.Data.Entity.AzureTableStorage.Requests;
using Microsoft.Data.Entity.AzureTableStorage.Utilities;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.AzureTableStorage.Query
{
    public partial class AtsQueryModelVisitor : EntityQueryModelVisitor
    {
        private readonly Dictionary<IQuerySource, SelectExpression> _queriesBySource
            = new Dictionary<IQuerySource, SelectExpression>();

        private readonly AtsQueryCompilationContext _queryCompilationContext;

        public AtsQueryModelVisitor([NotNull] AtsQueryCompilationContext queryCompilationContext)
            : base(queryCompilationContext)
        {
            _queryCompilationContext = queryCompilationContext;
        }

        private AtsQueryModelVisitor(AtsQueryModelVisitor visitor)
            : this(visitor._queryCompilationContext)
        {
        }

        [NotNull]
        public virtual SelectExpression GetSelectExpression([NotNull] IQuerySource key)
        {
            Check.NotNull(key, "key");
            return _queriesBySource[key];
        }

        public virtual bool TryGetSelectExpression([NotNull] IQuerySource key, [CanBeNull] out SelectExpression query)
        {
            Check.NotNull(key, "key");
            return _queriesBySource.TryGetValue(key, out query);
        }

        protected override ExpressionTreeVisitor CreateQueryingExpressionTreeVisitor(IQuerySource querySource)
        {
            return new AtsQueryingExpressionTreeVisitor(this, querySource);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            base.VisitWhereClause(whereClause, queryModel, index);

            foreach (var sourceQuery in _queriesBySource)
            {
                var filteringVisitor
                    = new FilteringExpressionTreeVisitor(this, sourceQuery.Key);

                sourceQuery.Value.Predicate = filteringVisitor.VisitExpression(whereClause.Predicate);
            }
        }

        private TResult BindMemberExpression<TResult>(
            MemberExpression memberExpression,
            IQuerySource querySource,
            Func<IProperty, SelectExpression, TResult> memberBinder)
        {
            var querySourceReferenceExpression
                = memberExpression.Expression as QuerySourceReferenceExpression;

            if (querySourceReferenceExpression != null
                && (querySource == null
                    || querySource == querySourceReferenceExpression.ReferencedQuerySource))
            {
                var entityType
                    = QueryCompilationContext.Model
                        .TryGetEntityType(
                            querySourceReferenceExpression.ReferencedQuerySource.ItemType);

                if (entityType != null)
                {
                    var property = entityType.TryGetProperty(memberExpression.Member.Name);

                    if (property != null)
                    {
                        SelectExpression selectExpression;
                        if (_queriesBySource
                            .TryGetValue(querySourceReferenceExpression.ReferencedQuerySource, out selectExpression))
                        {
                            return memberBinder(property, selectExpression);
                        }
                    }
                }
            }

            return default(TResult);
        }

        private static readonly MethodInfo _executeQueryMethodInfo
            = typeof(AtsQueryModelVisitor).GetTypeInfo().GetDeclaredMethod("ExecuteSelectExpression");

        [UsedImplicitly]
        private static IEnumerable<TResult> ExecuteSelectExpression<TResult>(QueryContext queryContext, IEntityType entityType, SelectExpression selectExpression)
            where TResult : class, new()
        {
            var context = ((AtsQueryContext)queryContext);
            var table = new AtsTable(entityType.TableName());
            var query = context.TableQueryGenerator.GenerateTableQuery(selectExpression);
            var request = new QueryTableRequest<TResult>(
                table,
                query, s =>
                    (TResult)context.StateManager.GetOrMaterializeEntry(
                        entityType,
                        context.ValueReaderFactory.Create(entityType, s)
                        ).Entity);

            return context.GetOrAddQueryResults(request);
        }
    }
}
