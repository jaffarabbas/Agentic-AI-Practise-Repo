using System.Data;
using Dapper;
using Pgvector;

namespace DocumentQA.Infrastructure.Persistence;

public class VectorTypeHandler : SqlMapper.TypeHandler<Vector>
{
    public override void SetValue(IDbDataParameter parameter, Vector? value)
    {
        if (value == null)
        {
            parameter.Value = DBNull.Value;
        }
        else
        {
            // Convert Vector to string format that PostgreSQL pgvector understands: "[x,y,z,...]"
            parameter.Value = value.ToString();
            parameter.DbType = DbType.Object;
        }
    }

    public override Vector Parse(object value)
    {
        if (value is float[] floatArray)
        {
            return new Vector(floatArray);
        }

        throw new InvalidCastException($"Cannot convert {value?.GetType()} to Vector");
    }
}
