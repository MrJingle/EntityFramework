// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;

namespace Microsoft.Data.Entity.ChangeTracking
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class CompositeEntityKey : EntityKey
    {
        private readonly IEntityType _entityType;
        private readonly object[] _keyValueParts;

        public CompositeEntityKey([NotNull] IEntityType entityType, [NotNull] object[] keyValueParts)
        {
            Check.NotNull(entityType, "entityType");
            Check.NotNull(keyValueParts, "keyValueParts");

            _entityType = entityType;
            _keyValueParts = keyValueParts;
        }

        public new virtual object[] Value
        {
            get { return _keyValueParts; }
        }

        protected override object GetValue()
        {
            return _keyValueParts;
        }

        private bool Equals(CompositeEntityKey other)
        {
            if (_entityType != other._entityType)
            {
                return false;
            }

            var parts = _keyValueParts;
            var otherParts = other._keyValueParts;

            var partCount = parts.Length;
            if (partCount != otherParts.Length)
            {
                return false;
            }

            for (var i = 0; i < partCount; i++)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(parts[i], otherParts[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return ReferenceEquals(this, obj)
                   || obj.GetType() == GetType()
                   && Equals((CompositeEntityKey)obj);
        }

        public override int GetHashCode()
        {
            return _keyValueParts.Aggregate(
                _entityType.GetHashCode() * 397,
                (t, v) => (t * 397) ^ (v != null ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(v) : 0));
        }

        [UsedImplicitly]
        private string DebuggerDisplay
        {
            get { return string.Format("{0}({1})", _entityType.Type.Name, string.Join(", ", _keyValueParts.Select(k => k.ToString()))); }
        }
    }
}
