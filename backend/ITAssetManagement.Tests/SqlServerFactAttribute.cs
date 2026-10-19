namespace ITAssetManagement.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")))
        {
            Skip = "Set ITAM_TEST_SQL_CONNECTION to run this SQL Server integration test.";
        }
    }
}
