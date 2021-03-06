// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.Data.Entity.Metadata
{
    public class ClrPropertySetterSourceTest
    {
        [Fact]
        public void Property_is_returned_if_it_implements_IClrPropertySetter()
        {
            var setterMock = new Mock<IClrPropertySetter>();
            var propertyMock = setterMock.As<IProperty>();

            var source = new ClrPropertySetterSource();

            Assert.Same(setterMock.Object, source.GetAccessor(propertyMock.Object));
        }

        [Fact]
        public void Delegate_setter_is_returned_for_IProperty_property()
        {
            var entityType = new EntityType(typeof(Customer));
            var idProperty = entityType.AddProperty("Id", typeof(int));

            var customer = new Customer { Id = 7 };

            new ClrPropertySetterSource().GetAccessor(idProperty).SetClrValue(customer, 77);

            Assert.Equal(77, customer.Id);
        }

        [Fact]
        public void Delegate_setter_is_returned_for_property_type_and_name()
        {
            var customer = new Customer { Id = 7 };

            new ClrPropertySetterSource().GetAccessor(typeof(Customer), "Id").SetClrValue(customer, 77);

            Assert.Equal(77, customer.Id);
        }

        [Fact]
        public void Delegate_setter_is_cached_by_type_and_property_name()
        {
            var entityType = new EntityType(typeof(Customer));
            var idProperty = entityType.AddProperty("Id", typeof(int));

            var source = new ClrPropertySetterSource();

            var accessor = source.GetAccessor(typeof(Customer), "Id");
            Assert.Same(accessor, source.GetAccessor(typeof(Customer), "Id"));
            Assert.Same(accessor, source.GetAccessor(idProperty));
        }

        #region Fixture

        private class Customer
        {
            internal int Id { get; set; }
        }

        #endregion
    }
}
