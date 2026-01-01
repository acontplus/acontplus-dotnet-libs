namespace Demo.Application.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IRepository<Usuario> _usuarioRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IAdoRepository _adoRepository;
        private readonly ISqlExceptionTranslator _exceptionTranslator;

        public UsuarioService(
            IUnitOfWork unitOfWork,
            ILogger<UsuarioService> logger,
            IAdoRepository adoRepository,
            ISqlExceptionTranslator exceptionTranslator)
        {
            _usuarioRepository = unitOfWork.GetRepository<Usuario>();
            _unitOfWork = unitOfWork;
            _logger = logger;
            _adoRepository = adoRepository;
            _exceptionTranslator = exceptionTranslator;
        }

        public async Task<Result<Usuario, DomainErrors>> AddAsync(Usuario usuario)
        {
            try
            {
                var existingUser = await _usuarioRepository.GetFirstOrDefaultAsync(x => x.Username == usuario.Username);

                if (existingUser is not null)
                {
                    return DomainErrors.FromSingle(DomainError.Conflict(
                        "USERNAME_EXISTS",
                        $"Username '{usuario.Username}' already exists"));
                }

                var addedUser = await _usuarioRepository.AddAsync(usuario);
                await _unitOfWork.SaveChangesAsync();

                return Result<Usuario, DomainErrors>.Success(addedUser);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error adding user {Username}", usuario.Username);
                return DomainErrors.FromSingle(DomainError.Internal(
                    "DB_INSERT_ERROR",
                    "Failed to create user due to database error",
                    details: new Dictionary<string, object> { ["username"] = usuario.Username }));
            }
        }

        public async Task<Result<int, DomainError>> CreateAsync()
        {
            try
            {
                var options = new CommandOptionsDto
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };

                var result = await _unitOfWork.AdoRepository.ExecuteNonQueryAsync(
                    "INSERT INTO Test.WorkerTest(Content) VALUES ('Inserting')",
                    options: options);

                return Result<int, DomainError>.Success(result);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Failed to execute raw SQL command");
                // Example: Use SqlResponseAdapter to map SQL error code to DomainError
                var sqlError = SqlResponseAdapter.MapSqlServerError(ex.ErrorCode.ToString(), ex.Message);
                return sqlError;
            }
        }

        public async Task<Result<bool, DomainError>> DeleteAsync(int id)
        {
            try
            {
                var userFound = await _usuarioRepository.GetByIdAsync(id);
                if (userFound == null)
                {
                    return DomainError.NotFound(
                        "USER_NOT_FOUND",
                        $"User with ID {id} not found");
                }

                userFound.MarkAsDeleted();
                await _usuarioRepository.UpdateAsync(userFound);
                await _unitOfWork.SaveChangesAsync();

                return Result<bool, DomainError>.Success(true);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error deleting user {UserId}", id);
                return DomainError.Internal(
                    "DB_DELETE_ERROR",
                    "Failed to delete user",
                    details: new Dictionary<string, object> { ["userId"] = id });
            }
        }

        public async Task<Result<List<UsuarioDto>, DomainError>> GetDynamicUserListAsync()
        {
            try
            {
                var options = new CommandOptionsDto
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };

                var result = await _unitOfWork.AdoRepository.QueryAsync<UsuarioDto>(
                    "SELECT * FROM dbo.Usuarios",
                    options: options);

                return Result<List<UsuarioDto>, DomainError>.Success(result);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Failed to execute raw SQL command");
                // Example: Use SqlResponseAdapter to map SQL error code to DomainError
                var sqlError = SqlResponseAdapter.MapSqlServerError(ex.ErrorCode.ToString(), ex.Message);
                return sqlError;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute raw SQL command");
                return DomainError.Internal(
                    "SQL_COMMAND_FAILED",
                    "Failed to execute database command");
            }
        }

        public async Task<Result<SpResponse, DomainError>> GetLegacySpResponseAsync()
        {
            try
            {
                var options = new CommandOptionsDto
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 30
                };
                var parameters = new Dictionary<string, object>
                {
                    ["@Id"] = 123
                };

                var result = await _adoRepository.QuerySingleOrDefaultAsync<SpResponse>(
                    "dbo.sp_Test",
                    parameters: parameters,
                    options: options);

                // Handle null result
                if (result == null)
                {
                    return DomainError.NotFound("SP_NO_RESULT", "Stored procedure returned no result");
                }

                // Example: If result has a SQL error code, map it to DomainError
                if (!result.IsSuccess && !string.IsNullOrEmpty(result.Code))
                {
                    var sqlError = SqlResponseAdapter.MapSqlServerError(result.Code, result.Message ?? "Unknown error");
                    return sqlError;
                }

                return result.IsSuccess
                    ? Result<SpResponse, DomainError>.Success(result)
                    : DomainError.Internal(result.Code ?? "UNKNOWN_ERROR", result.Message ?? "An error occurred");
            }
            catch (DbException ex)
            {
                var sqlError = SqlResponseAdapter.MapSqlServerError(ex.ErrorCode.ToString(), ex.Message);
                return sqlError;
            }
            catch (Exception ex)
            {
                var domainEx = _exceptionTranslator.Translate(ex);

                // Handle validation errors first
                if (domainEx.ErrorType == ErrorType.Validation)
                {
                    return
                        DomainError.Validation(
                            code: domainEx.ErrorCode,
                            message: domainEx.Message);
                }

                // Then handle transient errors
                if (_exceptionTranslator.IsTransient(ex))
                {
                    return DomainError.ServiceUnavailable(
                            code: "DB_TRANSIENT_ERROR",
                            message: "Database temporarily unavailable",
                            // shouldRetry: true,
                            details: new Dictionary<string, object>
                            {
                                ["procedure"] = "dbo.sp_Test",
                                ["error"] = ex.Message
                            });
                }

                // Fallback to internal error
                return
                    DomainError.Internal(
                        code: "SP_EXECUTION_ERROR",
                        message: "Failed to execute stored procedure",
                        details: new Dictionary<string, object>
                        {
                            ["procedure"] = "dbo.sp_Test",
                            ["error"] = ex.Message
                        });
            }
        }
        public async Task<Result<PagedResult<UsuarioDto>, DomainError>> GetPaginatedUsersAsync(
            PaginationRequest pagination)
        {
            try
            {
                Expression<Func<Usuario, bool>> filter = u => !u.IsDeleted;
                Expression<Func<Usuario, object>>? orderBy = u => u.CreatedAt;

                var pagedResult = await _usuarioRepository.GetPagedAsync(
                    pagination: pagination,
                    predicate: filter,
                    orderBy: orderBy,
                    orderByDescending: true);

                var userDtos = pagedResult.Items
                    .Select(ObjectMapper.Map<Usuario, UsuarioDto>)
                    .ToList();

                var links = pagedResult.BuildPaginationLinks(
                    baseRoute: "/api/users",
                    pageSize: pagination.PageSize);

                var metadata = new Dictionary<string, object>
                {
                    [PaginationMetadataKeys.HasFilters] = true,
                    [PaginationMetadataKeys.SortBy] = "CreatedAt",
                    [PaginationMetadataKeys.SortDirection] = "DESC",
                    [ApiMetadataKeys.Links] = links
                };

                return new PagedResult<UsuarioDto>(
                    items: userDtos.Where(u => u != null).Cast<UsuarioDto>(),
                    pageIndex: pagedResult.PageIndex,
                    pageSize: pagedResult.PageSize,
                    totalCount: pagedResult.TotalCount,
                    metadata: metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated users");
                return DomainError.Internal(
                    code: "PAGINATION_ERROR",
                    message: "Failed to retrieve paginated users",
                    details: new Dictionary<string, object>
                    {
                        [ApiMetadataKeys.PageIndex] = pagination.PageIndex,
                        [ApiMetadataKeys.PageSize] = pagination.PageSize
                    });
            }
        }

        public async Task<Result<Usuario, DomainError>> GetByIdAsync(int id)
        {
            try
            {
                var user = await _usuarioRepository.GetByIdAsync(id);
                return user == null
                    ? (Result<Usuario, DomainError>)DomainError.NotFound("USER_NOT_FOUND", $"User with ID {id} not found")
                    : Result<Usuario, DomainError>.Success(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by ID {UserId}", id);
                return DomainError.Internal("GET_USER_ERROR", "Failed to fetch user");
            }
        }

        public async Task<Result<SuccessWithWarnings<List<Usuario>>, DomainError>> ImportUsuariosAsync(List<UsuarioDto> dtos)
        {
            var warnings = new List<DomainError>();
            var imported = new List<Usuario>();
            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email))
                {
                    warnings.Add(DomainError.Validation("INVALID_DATA", $"Missing username or email for user: {dto.Username ?? "<null>"}"));
                    continue;
                }
                var existing = await _usuarioRepository.GetFirstOrDefaultAsync(x => x.Username == dto.Username);
                if (existing != null)
                {
                    warnings.Add(DomainError.Conflict("DUPLICATE_USER", $"Username '{dto.Username}' already exists"));
                    continue;
                }
                var usuario = ObjectMapper.Map<UsuarioDto, Usuario>(dto);
                if (usuario != null)
                {
                    await _usuarioRepository.AddAsync(usuario);
                    imported.Add(usuario);
                }
            }
            try
            {
                await _unitOfWork.SaveChangesAsync();
                var domainWarnings = warnings.Count > 0 ? DomainWarnings.Multiple(warnings) : DomainWarnings.Multiple(Array.Empty<DomainError>());
                return Result<SuccessWithWarnings<List<Usuario>>, DomainError>.Success(new SuccessWithWarnings<List<Usuario>>(imported, domainWarnings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing users");
                return DomainError.Internal("IMPORT_USERS_ERROR", "Failed to import users");
            }
        }

        public async Task<Result<Usuario, DomainErrors>> UpdateAsync(int id, Usuario usuario)
        {
            try
            {
                var userFound = await _usuarioRepository.GetByIdAsync(id);
                if (userFound == null)
                {
                    return DomainErrors.FromSingle(DomainError.NotFound(
                        "USER_NOT_FOUND",
                        $"User with ID {id} not found"));
                }

                // Validate username uniqueness if changed
                if (userFound.Username != usuario.Username)
                {
                    var usernameExists =
                        await _usuarioRepository.ExistsAsync(x => x.Username == usuario.Username && x.Id != id);

                    if (usernameExists)
                    {
                        return DomainErrors.FromSingle(DomainError.Conflict(
                            "USERNAME_EXISTS",
                            $"Username '{usuario.Username}' already exists"));
                    }
                }

                // Update properties
                userFound.Username = usuario.Username;
                userFound.Email = usuario.Email;
                // ... other properties

                await _usuarioRepository.UpdateAsync(userFound);
                await _unitOfWork.SaveChangesAsync();

                return Result<Usuario, DomainErrors>.Success(userFound);
            }
            catch (DbException ex)
            {
                _logger.LogError(ex, "Database error updating user {UserId}", id);
                return DomainErrors.FromSingle(DomainError.Internal(
                    "DB_UPDATE_ERROR",
                    "Failed to update user",
                    details: new Dictionary<string, object> { ["userId"] = id }));
            }
        }

        #region High-Performance ADO.NET Operations

        public async Task<Result<int, DomainError>> GetUserCountAsync()
        {
            try
            {
                var sql = "SELECT COUNT(*) FROM dbo.Usuario WHERE IsDeleted = 0";
                var count = await _adoRepository.ExecuteScalarAsync<int>(sql);
                return Result<int, DomainError>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user count");
                return DomainError.Internal("GET_USER_COUNT_ERROR", "Failed to get user count");
            }
        }

        public async Task<Result<bool, DomainError>> CheckUserExistsAsync(string username)
        {
            try
            {
                var sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario WHERE Username = @Username AND IsDeleted = 0) THEN 1 ELSE 0 END";
                var parameters = new Dictionary<string, object> { ["@Username"] = username };
                var exists = await _adoRepository.ExistsAsync(sql, parameters);
                return Result<bool, DomainError>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Username}", username);
                return DomainError.Internal("CHECK_USER_EXISTS_ERROR", "Failed to check user existence");
            }
        }

        public async Task<Result<long, DomainError>> GetActiveUsersCountAsync()
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*)
                    FROM dbo.Usuario
                    WHERE IsDeleted = 0
                    AND CreatedAt >= DATEADD(MONTH, -6, GETUTCDATE())";

                var count = await _adoRepository.LongCountAsync(sql);
                return Result<long, DomainError>.Success(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users count");
                return DomainError.Internal("GET_ACTIVE_COUNT_ERROR", "Failed to get active users count");
            }
        }

        public async Task<Result<PagedResult<Usuario>, DomainError>> GetPagedUsersAdoAsync(
            PaginationRequest pagination)
        {
            try
            {
                var baseSql = @"
                    SELECT Id, Username, Email, CreatedAt, UpdatedAt, IsDeleted
                    FROM dbo.Usuario
                    WHERE IsDeleted = 0";

                // Note: SearchTerm is already handled by the ADO repository via pagination.SearchTerm
                // No need to manually add it here - it will be added automatically as @__SearchTerm

                var result = await _adoRepository.GetPagedAsync<Usuario>(baseSql, pagination);
                return Result<PagedResult<Usuario>, DomainError>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users with ADO");
                return DomainError.Internal("GET_PAGED_ADO_ERROR", "Failed to get paged users");
            }
        }

        public async Task<Result<PagedResult<Usuario>, DomainError>> GetPagedUsersComplexAsync(
            PaginationRequest pagination,
            DateTime? createdAfter = null)
        {
            try
            {
                var baseSql = @"
                    SELECT Id, Username, Email, CreatedAt, UpdatedAt, IsDeleted
                    FROM dbo.Usuario
                    WHERE IsDeleted = 0";

                // If we have additional filter parameters, add them to pagination.Filters
                if (createdAfter.HasValue)
                {
                    baseSql += " AND CreatedAt >= @CreatedAfter";

                    // Merge additional filter into pagination
                    var filters = pagination.Filters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        ?? new Dictionary<string, object>();
                    filters["CreatedAfter"] = createdAfter.Value;

                    pagination = pagination with { Filters = filters };
                }

                var result = await _adoRepository.GetPagedAsync<Usuario>(baseSql, pagination);
                return Result<PagedResult<Usuario>, DomainError>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting complex paged users");
                return DomainError.Internal("GET_PAGED_COMPLEX_ERROR", "Failed to get complex paged users");
            }
        }

        public async Task<Result<PagedResult<Usuario>, DomainError>> GetPagedUsersFromStoredProcAsync(
            PaginationRequest pagination)
        {
            try
            {
                // Stored procedure will receive filters from pagination.Filters
                // No need to pass additional parameters - they're already in pagination
                var result = await _adoRepository.GetPagedFromStoredProcedureAsync<Usuario>(
                    "dbo.GetPagedUsuarios",
                    pagination);

                return Result<PagedResult<Usuario>, DomainError>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged users from stored procedure");
                return DomainError.Internal("GET_PAGED_SP_ERROR", "Failed to get users from stored procedure");
            }
        }

        public async Task<Result<int, DomainError>> BulkInsertUsersAsync(List<UsuarioDto> users)
        {
            try
            {
                var dataTable = new DataTable();
                dataTable.Columns.Add("Username", typeof(string));
                dataTable.Columns.Add("Email", typeof(string));
                dataTable.Columns.Add("CreatedAt", typeof(DateTime));
                dataTable.Columns.Add("UpdatedAt", typeof(DateTime));
                dataTable.Columns.Add("IsDeleted", typeof(bool));

                foreach (var user in users)
                {
                    dataTable.Rows.Add(
                        user.Username,
                        user.Email,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        false);
                }

                var insertedCount = await _adoRepository.BulkInsertAsync(dataTable, "dbo.Usuario");
                return Result<int, DomainError>.Success(insertedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk inserting users");
                return DomainError.Internal("BULK_INSERT_ERROR", "Failed to bulk insert users");
            }
        }

        public async Task<Result<int, DomainError>> ExecuteBatchOperationsAsync(List<int> userIds)
        {
            try
            {
                var commands = userIds.Select(id =>
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["@UserId"] = id,
                        ["@UpdatedAt"] = DateTime.UtcNow
                    };
                    return (
                        Sql: "UPDATE dbo.Usuario SET UpdatedAt = @UpdatedAt WHERE Id = @UserId AND IsDeleted = 0",
                        Parameters: (Dictionary<string, object>?)parameters
                    );
                });

                var affectedRows = await _adoRepository.ExecuteBatchNonQueryAsync(commands);
                return Result<int, DomainError>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch operations");
                return DomainError.Internal("BATCH_OPERATIONS_ERROR", "Failed to execute batch operations");
            }
        }

        #endregion

        #region Test Methods for Exception Handling

        /// <summary>
        /// Test method that throws an exception to verify middleware error handling.
        /// </summary>
        public async Task<Result<Usuario, DomainError>> GetUserWithExceptionAsync(int id)
        {
            // Simulate an exception in the application layer
            throw new InvalidOperationException("Test exception from application service");
        }

        /// <summary>
        /// Test method that returns a custom domain error.
        /// </summary>
        public async Task<Result<Usuario, DomainError>> GetUserWithCustomErrorAsync(int id)
        {
            try
            {
                var t = 4 / (id - 3); // Will throw DivideByZeroException if id == 3
                return new Usuario
                {
                    Id = id,
                    Username = "testuser",
                    Email = ""
                };
            }
            catch (Exception ex)
            {
                return DomainError.Internal(
               code: "CUSTOM_TEST_ERROR",
               message: "This is a custom test error from the service" + ex.Message ?? "",
               details: new Dictionary<string, object>
               {
                   ["userId"] = id,
                   ["testType"] = "custom_error"
               }
           );
            }

        }

        #endregion
    }
}

