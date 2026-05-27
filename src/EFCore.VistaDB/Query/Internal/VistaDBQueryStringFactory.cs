// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.EntityFrameworkCore.VistaDB.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class VistaDBQueryStringFactory : IRelationalQueryStringFactory
{
    private readonly IRelationalTypeMappingSource _typeMapper;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public VistaDBQueryStringFactory(IRelationalTypeMappingSource typeMapper)
        => _typeMapper = typeMapper;

    // VistaDB: no analog — SqlServer's factory inspects SqlParameter.SqlDbType to emit precise
    // T-SQL "DECLARE @p sqlType = literal;" prefixes with size/precision/scale annotations. The
    // VistaDB ADO.NET provider does not expose an equivalent typed parameter discriminator, so we
    // render a generic DECLARE that mirrors the command's parameter list using the mapped store type.
    // Original SqlServer logic (TypeNameBuilder + SqlDbType switch) preserved below for future revival.
    /*
        public virtual string Create(DbCommand command) { ... full SqlDbType switch ... }
        internal static class TypeNameBuilder { ... AppendSize/Precision/Scale helpers ... }
    */

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual string Create(DbCommand command)
    {
        if (command.Parameters.Count == 0)
        {
            return command.CommandText;
        }

        var builder = new StringBuilder();
        foreach (DbParameter parameter in command.Parameters)
        {
            builder
                .Append("DECLARE ")
                .Append(parameter.ParameterName)
                .Append(" = ");

            if (parameter.Value == DBNull.Value || parameter.Value is null)
            {
                builder.Append("NULL");
            }
            else
            {
                var typeMapping = _typeMapper.FindMapping(parameter.Value.GetType());

                builder.Append(
                    typeMapping != null
                        ? typeMapping.GenerateSqlLiteral(parameter.Value)
                        : parameter.Value.ToString());
            }

            builder.AppendLine(";");
        }

        return builder
            .AppendLine()
            .Append(command.CommandText).ToString();
    }
}
