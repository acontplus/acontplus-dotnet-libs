namespace Demo.Application.Services;

public class AtsService(IUnitOfWork uow) : IAtsService
{
    private readonly IAdoRepository _adoRepository = uow.AdoRepository;
    private const string ModuleName = "Billing.Ats_";

    public async Task<SpResponse?> CheckValidationAsync(Dictionary<string, object> parameters)
    {
        return await _adoRepository.QuerySingleOrDefaultAsync<SpResponse>(
            $"{ModuleName}CheckValidation",
            parameters
        );
    }

    public async Task<DataSet> GetAsync(Dictionary<string, object> parameters)
    {
        var options = new CommandOptionsDto
        {
            CommandTimeout = 0
        };

        return await _adoRepository.GetDataSetAsync(
            $"{ModuleName}Get",
            parameters,
            options
        );
    }
}

