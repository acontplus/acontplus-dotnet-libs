namespace Demo.Application.Services;


public class ReportService(IAdoRepository adoRepository) : IReportService
{
    public async Task<DataSet> GetParamsAsync(Dictionary<string, object> parameters)
    {
        return await adoRepository.GetDataSetAsync("Reporte.Report_Get", parameters);
    }

    public async Task<DataSet> GetDataAsync(string spname, Dictionary<string, object> parameters, bool withTableNames = false)
    {
        var options = new CommandOptionsDto
        {
            CommandType = CommandType.StoredProcedure,
            WithTableNames = withTableNames// Assuming you want to disable timeout for report queries
        };
        return await adoRepository.GetDataSetAsync(spname, parameters, options);
    }
}

