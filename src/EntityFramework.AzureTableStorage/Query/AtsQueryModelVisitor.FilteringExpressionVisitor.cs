﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Data.Entity.AzureTableStorage.Query.Expressions;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.AzureTableStorage.Query
{
    public partial class AtsQueryModelVisitor
    {
        protected class FilteringExpressionTreeVisitor : ThrowingExpressionTreeVisitor
        {
            private readonly AtsQueryModelVisitor _queryModelVisitor;
            private readonly IQuerySource _querySource;

            public FilteringExpressionTreeVisitor(AtsQueryModelVisitor queryModelVisitor, IQuerySource querySource)
            {
                _queryModelVisitor = queryModelVisitor;
                _querySource = querySource;
            }

            protected override Expression VisitBinaryExpression(BinaryExpression expression)
            {
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    {
                        return UnfoldStructuralComparison(expression.NodeType, ProcessComparisonExpression(expression));
                    }
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    {
                        return ProcessComparisonExpression(expression);
                    }

                    case ExpressionType.AndAlso:
                    {
                        var left = VisitExpression(expression.Left);
                        var right = VisitExpression(expression.Right);

                        return left != null
                               && right != null
                            ? Expression.AndAlso(left, right)
                            : (left ?? right);
                    }

                    case ExpressionType.OrElse:
                    {
                        var left = VisitExpression(expression.Left);
                        var right = VisitExpression(expression.Right);

                        return left != null
                               && right != null
                            ? Expression.OrElse(left, right)
                            : null;
                    }

                    default:
                        return null;
                }
            }

            protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
            {
                return null;
            }

            protected override Expression VisitMemberExpression(MemberExpression expression)
            {
                if (expression.Type == typeof(bool))
                {
                    return VisitExpression(
                        Expression.MakeBinary(
                            ExpressionType.Equal,
                            expression,
                            Expression.Constant(true)));
                }

                return BindOperand(expression);
            }

            protected override Expression VisitConstantExpression(ConstantExpression expression)
            {
                return null;
            }

            protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
            {
                return null;
            }

            protected override Expression VisitUnaryExpression(UnaryExpression expression)
            {
                if (expression.Type == typeof(bool))
                {
                    return VisitExpression(
                        Expression.MakeBinary(
                            ExpressionType.Equal,
                            expression.Operand,
                            Expression.Constant(expression.NodeType != ExpressionType.Not)));
                }
                return null;
            }

            private Expression ProcessComparisonExpression(BinaryExpression expression)
            {
                var leftExpression = BindOperand(expression.Left);
                var rightExpression = BindOperand(expression.Right);

                if (leftExpression == null
                    || rightExpression == null)
                {
                    return null;
                }

                var propertyExpression = leftExpression as PropertyExpression
                                         ?? rightExpression as PropertyExpression;
                if (propertyExpression == null)
                {
                    return null;
                }

                var constantExpression = leftExpression as QueryableConstantExpression
                                         ?? rightExpression as QueryableConstantExpression;
                if (constantExpression == null)
                {
                    return null;
                }

                if (propertyExpression.PropertyName == "PartitionKey"
                    || propertyExpression.PropertyName == "RowKey")
                {
                    constantExpression.IsStringProperty = true;
                }
                if (ReferenceEquals(leftExpression, propertyExpression))
                {
                    return Expression
                        .MakeBinary(expression.NodeType, leftExpression, rightExpression);
                }
                var nodeType = FlipInequality(expression.NodeType);
                return Expression
                    .MakeBinary(nodeType, rightExpression, leftExpression);
            }

            private Expression UnfoldStructuralComparison(ExpressionType expressionType, Expression expression)
            {
                var binaryExpression = expression as BinaryExpression;

                if (binaryExpression != null)
                {
                    var leftConstantExpression = binaryExpression.Left as QueryableConstantExpression;

                    if (leftConstantExpression != null)
                    {
                        var leftExpressions = leftConstantExpression.Value as Expression[];

                        if (leftExpressions != null)
                        {
                            var rightConstantExpression = binaryExpression.Right as QueryableConstantExpression;

                            if (rightConstantExpression != null)
                            {
                                var rightExpressions = rightConstantExpression.Value as Expression[];

                                if (rightExpressions != null
                                    && leftExpressions.Length == rightExpressions.Length)
                                {
                                    return leftExpressions
                                        .Zip(rightExpressions, (l, r) =>
                                            Expression.MakeBinary(expressionType, l, r))
                                        .Aggregate((e1, e2) =>
                                            expressionType == ExpressionType.Equal
                                                ? Expression.AndAlso(e1, e2)
                                                : Expression.OrElse(e1, e2));
                                }
                            }
                        }
                    }
                }

                return expression;
            }

            private ExpressionType FlipInequality(ExpressionType nodeType)
            {
                switch (nodeType)
                {
                    case ExpressionType.GreaterThan:
                        return ExpressionType.LessThan;
                    case ExpressionType.GreaterThanOrEqual:
                        return ExpressionType.LessThanOrEqual;
                    case ExpressionType.LessThan:
                        return ExpressionType.GreaterThan;
                    case ExpressionType.LessThanOrEqual:
                        return ExpressionType.GreaterThanOrEqual;
                    default:
                        return nodeType;
                }
            }

            private Expression BindOperand(Expression expression)
            {
                var memberExpression = expression as MemberExpression;

                if (memberExpression != null)
                {
                    return _queryModelVisitor
                        .BindMemberExpression(
                            memberExpression,
                            _querySource,
                            (property, selectExpression)
                                => new PropertyExpression(property));
                }
                var constantExpression = expression as ConstantExpression;
                if (constantExpression != null
                    && constantExpression.Value != null)
                {
                    return new QueryableConstantExpression(constantExpression.Type, constantExpression.Value);
                }
                return null;
            }

            protected override Exception CreateUnhandledItemException<T>(T unhandledItem, string visitMethod)
            {
                throw new NotImplementedException("Filter expression not handled: " + unhandledItem.GetType().Name);
            }
        }
    }
}
