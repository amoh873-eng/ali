using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ERPSystem.Application.Services;

/// <summary>
/// مولّد أرقام تسلسلية آمن ضد التزامن (Concurrency-safe).
///
/// يستخدم جدول NumberSequences مع عملية MERGE ذرّية (HOLDLOCK) بحيث:
///  1) عند إنشاء قيدين في نفس الطلب، كل استدعاء يزيد العدّاد فوراً ويحصل على قيمة مختلفة.
///  2) عند وصول طلبين متزامنين من مستخدمين مختلفين، القفل يضمن أن كلاً منهما يقرأ قيمة فريدة.
///
/// هذه الآلية ذرّية بطبيعتها ولا تعتمد على CountAsync (الذي يقرأ السجلات المحفوظة فقط).
/// </summary>
public static class NumberSequenceHelper
{
    /// <summary>
    /// ينتج الرقم التالي بالصيغة "{prefix}-{yyyyMMdd}-{value:D4}".
    /// </summary>
    public static async Task<string> NextAsync(DbContext context, string prefix)
    {
        var key = $"{prefix}-{DateTime.Now:yyyyMMdd}";
        var next = await GetNextValueAsync(context, key);
        return $"{prefix}-{DateTime.Now:yyyyMMdd}-{next:D4}";
    }

    /// <summary>
    /// يولّد دفعة من الأرقام التسلسلية في استدعاء ذرّي واحد (العدد = count)
    /// بدل count عملية MERGE منفصلة — للاستيراد الجماعي للأصناف (5000+ صف).
    /// يستخدم نفس الآلية والجدول (NumberSequences) فلا يعتبر منطق ترقيم جديداً.
    /// </summary>
    public static async Task<IReadOnlyList<string>> NextBatchAsync(DbContext context, string prefix, int count)
    {
        if (count <= 0) return Array.Empty<string>();

        var key = $"{prefix}-{DateTime.Now:yyyyMMdd}";
        var next = await GetNextValueBatchAsync(context, key, count);
        var date = DateTime.Now.ToString("yyyyMMdd");

        var codes = new string[count];
        for (var i = 0; i < count; i++)
            codes[i] = $"{prefix}-{date}-{next - count + 1 + i:D4}";
        return codes;
    }

    /// <summary>
    /// يزيد العدّاد ذرياً ويعيد القيمة الجديدة.
    /// </summary>
    private static async Task<long> GetNextValueAsync(DbContext context, string key)
    {
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = """
                INSERT INTO "NumberSequences" ("Id", "SequenceKey", "LastValue")
                VALUES (@id, @key, 1)
                ON CONFLICT ("SequenceKey")
                DO UPDATE SET "LastValue" = "NumberSequences"."LastValue" + 1
                RETURNING "LastValue";
                """;

            var idParam = command.CreateParameter();
            idParam.ParameterName = "id";
            idParam.Value = Guid.NewGuid();
            command.Parameters.Add(idParam);

            var parameter = command.CreateParameter();
            parameter.ParameterName = "key";
            parameter.Value = key;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// يزيد العدّاد ذرياً بمقدار count ويعيد القيمة الجديدة (كتلة متسلسلة كاملة).
    /// </summary>
    private static async Task<long> GetNextValueBatchAsync(DbContext context, string key, int count)
    {
        var connection = context.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = """
                INSERT INTO "NumberSequences" ("Id", "SequenceKey", "LastValue")
                VALUES (@id, @key, @count)
                ON CONFLICT ("SequenceKey")
                DO UPDATE SET "LastValue" = "NumberSequences"."LastValue" + @count
                RETURNING "LastValue";
                """;

            var idParam = command.CreateParameter();
            idParam.ParameterName = "id";
            idParam.Value = Guid.NewGuid();
            command.Parameters.Add(idParam);

            var keyParam = command.CreateParameter();
            keyParam.ParameterName = "key";
            keyParam.Value = key;
            command.Parameters.Add(keyParam);

            var countParam = command.CreateParameter();
            countParam.ParameterName = "count";
            countParam.Value = count;
            command.Parameters.Add(countParam);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }
        finally
        {
            if (wasClosed && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}
