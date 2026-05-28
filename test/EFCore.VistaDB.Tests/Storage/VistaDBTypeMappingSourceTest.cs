// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.VistaDB.Storage.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Storage;

public class VistaDBTypeMappingSourceTest : RelationalTypeMappingSourceTestBase
{
    [ConditionalTheory,
     InlineData(typeof(bool), "bit", DbType.Boolean),
     InlineData(typeof(byte), "tinyint", DbType.Byte),
     InlineData(typeof(short), "smallint", DbType.Int16),
     InlineData(typeof(int), "int", DbType.Int32),
     InlineData(typeof(long), "bigint", DbType.Int64),
     InlineData(typeof(double), "float", DbType.Double),
     InlineData(typeof(float), "real", DbType.Single),
     InlineData(typeof(DateTime), "datetime2", DbType.DateTime2),
     InlineData(typeof(DateOnly), "date", DbType.Date),
     InlineData(typeof(TimeOnly), "time", DbType.Time),
     InlineData(typeof(DateTimeOffset), "datetimeoffset", DbType.DateTimeOffset),
     InlineData(typeof(Guid), "uniqueidentifier", DbType.Guid)]
    public void Can_map_by_clr_type(Type clrType, string expectedStoreType, DbType expectedDbType)
    {
        var mapping = GetTypeMapping(clrType);
        Assert.Equal(expectedStoreType, mapping.StoreType);
        Assert.Equal(expectedDbType, mapping.DbType);

        if (clrType.IsValueType)
        {
            mapping = GetTypeMapping(typeof(Nullable<>).MakeGenericType(clrType));
            Assert.Equal(expectedStoreType, mapping.StoreType);
            Assert.Equal(expectedDbType, mapping.DbType);
        }
    }

    [ConditionalFact]
    public void Default_decimal_mapping_uses_precision_18_scale_2()
    {
        var mapping = GetTypeMapping(typeof(decimal));
        Assert.Equal(DbType.Decimal, mapping.DbType);
        Assert.Equal("decimal(18,2)", mapping.StoreType);
    }

    [ConditionalFact]
    public void Default_string_mapping_uses_nvarchar_max()
    {
        var mapping = GetTypeMapping(typeof(string));
        Assert.Equal(DbType.String, mapping.DbType);
        Assert.Equal("nvarchar(max)", mapping.StoreType);
    }

    [ConditionalFact]
    public void Default_byte_array_mapping_uses_varbinary_max()
    {
        var mapping = GetTypeMapping(typeof(byte[]));
        Assert.Equal(DbType.Binary, mapping.DbType);
        Assert.Equal("varbinary(max)", mapping.StoreType);
    }

    protected override IRelationalTypeMappingSource CreateRelationalTypeMappingSource(IModel model)
    {
        var typeMappingSource = new VistaDBTypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<VistaDBSingletonOptions>());

        model.ModelDependencies = new RuntimeModelDependencies(typeMappingSource, null!, null!);
        return typeMappingSource;
    }

    protected override ModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configureConventions = null)
        => VistaDBTestHelpers.Instance.CreateConventionBuilder(configureConventions: configureConventions);
}
