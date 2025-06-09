using Azure.Data.Tables;

public class TableClientFactory
{
    private readonly IConfiguration _configuration;

    public TableClientFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TableClient GetClient(string tableName)
    {
        var connectionString = _configuration.GetConnectionString("AzureStorage") 
            ?? _configuration["AzureStorage:ConnectionString"];
        return new TableClient(connectionString, tableName);
    }
}