// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Data.Entity.Metadata
{
    public class KeyTest
    {
        #region Fixture

        public class Customer
        {
            public static PropertyInfo IdProperty = typeof(Customer).GetProperty("Id");
            public static PropertyInfo NameProperty = typeof(Customer).GetProperty("Name");

            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion

        [Fact]
        public void Can_create_key_from_properties()
        {
            var entityType = new EntityType("E");
            var property1 = entityType.AddProperty(Customer.IdProperty);
            var property2 = entityType.AddProperty(Customer.NameProperty);

            var key = new Key(new[] { property1, property2 });

            Assert.True(new[] { property1, property2 }.SequenceEqual(key.Properties));
        }

        [Fact]
        public void Validates_properties_from_same_entity()
        {
            var entityType = new EntityType("E");
            var property1 = entityType.AddProperty(Customer.IdProperty);
            var property2 = entityType.AddProperty(Customer.NameProperty);

            property1.EntityType = new EntityType("E1");
            property2.EntityType = new EntityType("E2");

            Assert.Equal(Strings.FormatInconsistentEntityType("properties"),
                Assert.Throws<ArgumentException>(
                    () => new Key(new[] { property1, property2 })).Message);
        }
    }
}
