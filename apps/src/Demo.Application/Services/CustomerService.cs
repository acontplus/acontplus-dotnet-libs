namespace Demo.Application.Services;

public class CustomerService(
    IAdoRepository adoRepository,
    ISqlExceptionTranslator sqlExceptionTranslator)
    : ICustomerService
{
    public async Task<Result<CustomerDto, DomainErrors>> GetByIdCardAsync(string idCard)
    {
        try
        {
            // Validate ID card length first
            if (idCard.Length is not (10 or 13))
            {
                var error = DomainError.Validation(
                    code: "INVALID_ID_CARD",
                    message: "The provided ID card is invalid. It must be either 10 or 13 characters long.",
                    target: nameof(idCard),
                    details: new Dictionary<string, object> { ["actualLength"] = idCard.Length });

                return Result<CustomerDto, DomainErrors>.Failure(error);
            }

            var parameters = new Dictionary<string, object> { { "idCard", idCard } };
            var customer =
                await adoRepository.QuerySingleOrDefaultAsync<CustomerDto>("dbo.GetCustomerByIdCard", parameters);

            return customer == null
                ? Result<CustomerDto, DomainErrors>.Failure(DomainError.NotFound("CUSTOMER_NOT_FOUND", "No customer found with the provided ID card."))
                : Result<CustomerDto, DomainErrors>.Success(customer);
        }
        catch (Exception ex)
        {
            var domainEx = sqlExceptionTranslator.Translate(ex);

            // Handle validation errors
            if (domainEx.ErrorType == ErrorType.Validation)
            {
                var validationErrors = new List<DomainError>
                {
                    DomainError.Validation(
                        code: "ID_CARD_VALIDATION_ERROR",
                        message: "The provided ID card is invalid.",
                        target: nameof(idCard),
                        details: new Dictionary<string, object>
                        {
                            ["idCard"] = idCard,
                            ["error"] = domainEx.Message
                        })
                };

                // If you have multiple validation errors, you can add them to the list
                // validationErrors.Add(anotherError);

                return Result<CustomerDto, DomainErrors>.Failure(DomainErrors.Multiple(validationErrors));
            }

            // Handle transient errors
            if (sqlExceptionTranslator.IsTransient(ex))
            {
                var transientError = DomainError.ServiceUnavailable(
                    code: "DB_TRANSIENT_ERROR",
                    message: "Database temporarily unavailable",
                    details: new Dictionary<string, object>
                    {
                        ["procedure"] = "dbo.sp_Test",
                        ["error"] = ex.Message
                    });

                return Result<CustomerDto, DomainErrors>.Failure(transientError);
            }

            // Fallback to internal error
            var internalError = DomainError.Internal(
                code: "SP_EXECUTION_ERROR",
                message: "Failed to execute stored procedure",
                details: new Dictionary<string, object>
                {
                    ["procedure"] = "dbo.sp_Test",
                    ["error"] = ex.Message
                });

            return Result<CustomerDto, DomainErrors>.Failure(internalError);
        }
    }
}
