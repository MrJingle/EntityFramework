// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Utilities;
using Xunit;

namespace Microsoft.Data.Entity.Design.Tests
{
    public class CSharpModelCodeGeneratorTest
    {
        [Fact]
        public void Generate_empty_model()
        {
            var builder = new ModelBuilder();

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_empty_model_with_annotations()
        {
            var builder = new ModelBuilder()
                .Annotation("A1", "V1")
                .Annotation("A2", "V2");

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder()
    .Annotation(""A1"", ""V1"")
    .Annotation(""A2"", ""V2"");

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_single_property()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_shadow_property()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property<int>("Id", shadowProperty: true);
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"", shadowProperty: true);
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_concurrency_token()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property<int>("Id", concurrencyToken: true);
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"", concurrencyToken: true);
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_shadow_property_and_concurrency_token()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property<int>("Id", shadowProperty: true, concurrencyToken: true);
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"", shadowProperty: true, concurrencyToken: true);
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_single_property_with_annotations()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id)
                        .Annotation("A1", "V1")
                        .Annotation("A2", "V2");
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"")
            .Annotation(""A1"", ""V1"")
            .Annotation(""A2"", ""V2"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_multiple_properties()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_multiple_properties_with_annotations()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id)
                        .Annotation("A1", "V1")
                        .Annotation("A2", "V2");
                    b.Property(e => e.Name)
                        .Annotation("A1", "V1")
                        .Annotation("A2", "V2");
                    b.Key(e => e.Id);
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"")
            .Annotation(""A1"", ""V1"")
            .Annotation(""A2"", ""V2"");
        b.Property<string>(""Name"")
            .Annotation(""A1"", ""V1"")
            .Annotation(""A2"", ""V2"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_composite_key()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(k => k.Properties(e => new { e.Id, e.Name })
                        .Annotation("A1", "V1")
                        .Annotation("A2", "V2"));
                });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(k => k.Properties(""Id"", ""Name"")
            .Annotation(""A1"", ""V1"")
            .Annotation(""A2"", ""V2""));
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_single_foreign_key()
        {
            var builder = new ModelBuilder();

            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.CustomerId);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>()
                .ForeignKeys(fks => fks.ForeignKey<Customer>(e => e.CustomerId));

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"");
    });

builder.Entity(""Order"", b =>
    {
        b.Property<int>(""CustomerId"");
        b.Property<int>(""Id"");
        b.Key(""Id"");
        b.ForeignKeys(fks => fks.ForeignKey(""Customer"", ""CustomerId""));
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_single_composite_foreign_key()
        {
            var builder = new ModelBuilder();

            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => new { e.Id, e.Name });
                });

            builder.Entity<Order>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.CustomerId);
                    b.Property(e => e.CustomerName);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>()
                .ForeignKeys(fks => fks.ForeignKey<Customer>(e => new { e.CustomerId, e.CustomerName }));

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"", ""Name"");
    });

builder.Entity(""Order"", b =>
    {
        b.Property<int>(""CustomerId"");
        b.Property<string>(""CustomerName"");
        b.Property<int>(""Id"");
        b.Key(""Id"");
        b.ForeignKeys(fks => fks.ForeignKey(""Customer"", ""CustomerId"", ""CustomerName""));
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_single_foreign_key_with_annotations()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.CustomerId);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>()
                .ForeignKeys(fks => fks.ForeignKey<Customer>(e => e.CustomerId)
                    .Annotation("A1", "V1")
                    .Annotation("A2", "V2"));

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"");
    });

builder.Entity(""Order"", b =>
    {
        b.Property<int>(""CustomerId"");
        b.Property<int>(""Id"");
        b.Key(""Id"");
        b.ForeignKeys(fks => fks.ForeignKey(""Customer"", ""CustomerId"")
            .Annotation(""A1"", ""V1"")
            .Annotation(""A2"", ""V2""));
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_multiple_foreign_keys()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => new { e.Id, e.Name });
                });

            builder.Entity<Order>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.CustomerId);
                    b.Property(e => e.CustomerName);
                    b.Property(e => e.ProductId);
                    b.Key(e => e.Id);
                });

            builder.Entity<Product>(b =>
                {
                    b.Property(e => e.Id);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>()
                .ForeignKeys(
                    fks =>
                        {
                            fks.ForeignKey<Customer>(e => new { e.CustomerId, e.CustomerName });
                            fks.ForeignKey<Product>(e => e.ProductId);
                        });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"", ""Name"");
    });

builder.Entity(""Order"", b =>
    {
        b.Property<int>(""CustomerId"");
        b.Property<string>(""CustomerName"");
        b.Property<int>(""Id"");
        b.Property<int>(""ProductId"");
        b.Key(""Id"");
        b.ForeignKeys(fks => 
            {
                fks.ForeignKey(""Customer"", ""CustomerId"", ""CustomerName"");
                fks.ForeignKey(""Product"", ""ProductId"");
            });
    });

builder.Entity(""Product"", b =>
    {
        b.Property<int>(""Id"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_entity_type_with_multiple_foreign_keys_with_annotations()
        {
            var builder = new ModelBuilder();
            builder.Entity<Customer>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.Name);
                    b.Key(e => new { e.Id, e.Name });
                });

            builder.Entity<Order>(b =>
                {
                    b.Property(e => e.Id);
                    b.Property(e => e.CustomerId);
                    b.Property(e => e.CustomerName);
                    b.Property(e => e.ProductId);
                    b.Key(e => e.Id);
                });

            builder.Entity<Product>(b =>
                {
                    b.Property(e => e.Id);
                    b.Key(e => e.Id);
                });

            builder.Entity<Order>()
                .ForeignKeys(
                    fks =>
                        {
                            fks.ForeignKey<Customer>(e => new { e.CustomerId, e.CustomerName })
                                .Annotation("A1", "V1")
                                .Annotation("A2", "V2");
                            fks.ForeignKey<Product>(e => e.ProductId)
                                .Annotation("A3", "V3")
                                .Annotation("A4", "V4");
                        });

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().Generate(builder.Model, stringBuilder);

            Assert.Equal(
                @"var builder = new ModelBuilder();

builder.Entity(""Customer"", b =>
    {
        b.Property<int>(""Id"");
        b.Property<string>(""Name"");
        b.Key(""Id"", ""Name"");
    });

builder.Entity(""Order"", b =>
    {
        b.Property<int>(""CustomerId"");
        b.Property<string>(""CustomerName"");
        b.Property<int>(""Id"");
        b.Property<int>(""ProductId"");
        b.Key(""Id"");
        b.ForeignKeys(fks => 
            {
                fks.ForeignKey(""Customer"", ""CustomerId"", ""CustomerName"")
                    .Annotation(""A1"", ""V1"")
                    .Annotation(""A2"", ""V2"");
                fks.ForeignKey(""Product"", ""ProductId"")
                    .Annotation(""A3"", ""V3"")
                    .Annotation(""A4"", ""V4"");
            });
    });

builder.Entity(""Product"", b =>
    {
        b.Property<int>(""Id"");
        b.Key(""Id"");
    });

return builder.Model;",
                stringBuilder.ToString());
        }

        [Fact]
        public void Generate_model_snapshot_class()
        {
            var model = new Model();
            var entityType = new EntityType("Entity");

            entityType.SetKey(entityType.AddProperty("Id", typeof(int)));
            model.AddEntityType(entityType);

            var stringBuilder = new IndentedStringBuilder();
            new CSharpModelCodeGenerator().GenerateModelSnapshotClass("MyNamespace", "MyClass", model, stringBuilder);

            Assert.Equal(
                @"using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations.Infrastructure;
using System;

namespace MyNamespace
{
    public class MyClass : ModelSnapshot
    {
        public override IModel Model
        {
            get
            {
                var builder = new ModelBuilder();
                
                builder.Entity(""Entity"", b =>
                    {
                        b.Property<int>(""Id"");
                        b.Key(""Id"");
                    });
                
                return builder.Model;
            }
        }
    }
}",
                stringBuilder.ToString());
        }

        private class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
            public string CustomerName { get; set; }
            public int ProductId { get; set; }
        }

        private class Product
        {
            public int Id { get; set; }
        }
    }
}
