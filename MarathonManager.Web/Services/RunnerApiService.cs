using MarathonManager.API.DTOs;
using System.Net.Http.Headers;

namespace MarathonManager.Web.Services
{
    /// <summary>
    /// Implementation of Runner API Service
    /// Calls the MarathonManager.API endpoints
    /// </summary>
    public class RunnerApiService : IRunnerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RunnerApiService> _logger;

        public RunnerApiService(
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RunnerApiService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Set Authorization header with JWT token from cookies
        /// </summary>
        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// GET: /api/Races/runner/dashboard
        /// </summary>
        public async Task<ApiResponse<RunnerDashboardDto>> GetDashboardAsync()
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync("/api/Races/runner/dashboard");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<RunnerDashboardDto>>();
                    return result ?? new ApiResponse<RunnerDashboardDto>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<RunnerDashboardDto>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetDashboard API");
                return new ApiResponse<RunnerDashboardDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching dashboard data",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// GET: /api/Races/runner/available
        /// </summary>
        public async Task<ApiResponse<PaginatedResponse<AvailableRaceDto>>> GetAvailableRacesAsync(
            int pageNumber = 1,
            int pageSize = 6)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync(
                    $"/api/Races/runner/available?pageNumber={pageNumber}&pageSize={pageSize}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<AvailableRaceDto>>>();
                    return result ?? new ApiResponse<PaginatedResponse<AvailableRaceDto>>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<PaginatedResponse<AvailableRaceDto>>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetAvailableRaces API");
                return new ApiResponse<PaginatedResponse<AvailableRaceDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching available races",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// GET: /api/Races/{id}/details
        /// </summary>
        public async Task<ApiResponse<RaceDetailsDto>> GetRaceDetailsAsync(int raceId)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync($"/api/Races/{raceId}/details");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<RaceDetailsDto>>();
                    return result ?? new ApiResponse<RaceDetailsDto>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<RaceDetailsDto>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetRaceDetails API for race {RaceId}", raceId);
                return new ApiResponse<RaceDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching race details",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// POST: /api/Races/runner/register
        /// </summary>
        public async Task<ApiResponse<MyRegistrationDto>> RegisterForRaceAsync(RegisterForRaceRequest request)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync("/api/Races/runner/register", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<MyRegistrationDto>>();
                    return result ?? new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                // Try to read error message from response
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<MyRegistrationDto>>();
                    if (errorResponse != null)
                        return errorResponse;
                }
                catch { }

                return new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = $"Registration failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling RegisterForRace API");
                return new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// GET: /api/Races/runner/registrations
        /// </summary>
        public async Task<ApiResponse<PaginatedResponse<MyRegistrationDto>>> GetMyRegistrationsAsync(
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync(
                    $"/api/Races/runner/registrations?pageNumber={pageNumber}&pageSize={pageSize}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<MyRegistrationDto>>>();
                    return result ?? new ApiResponse<PaginatedResponse<MyRegistrationDto>>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<PaginatedResponse<MyRegistrationDto>>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetMyRegistrations API");
                return new ApiResponse<PaginatedResponse<MyRegistrationDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching registrations",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// DELETE: /api/Races/runner/registrations/{id}
        /// </summary>
        public async Task<ApiResponse<object>> CancelRegistrationAsync(int registrationId)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.DeleteAsync($"/api/Races/runner/registrations/{registrationId}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                    return result ?? new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                // Try to read error message from response
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                    if (errorResponse != null)
                        return errorResponse;
                }
                catch { }

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Cancellation failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CancelRegistration API");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while cancelling registration",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// GET: /api/Races/runner/results
        /// </summary>
        public async Task<ApiResponse<PaginatedResponse<MyResultDto>>> GetMyResultsAsync(
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync(
                    $"/api/Races/runner/results?pageNumber={pageNumber}&pageSize={pageSize}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<MyResultDto>>>();
                    return result ?? new ApiResponse<PaginatedResponse<MyResultDto>>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<PaginatedResponse<MyResultDto>>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetMyResults API");
                return new ApiResponse<PaginatedResponse<MyResultDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching results",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
