using MarathonManager.API.DTOs;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace MarathonManager.Web.Services
{
    /// <summary>
    /// Implementation of Runner API Service
    /// Calls the MarathonManager.API endpoints
    /// </summary>
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
        /// POST: /api/Races/runner/registrations/{registrationId}/fake-result
        /// </summary>
        public async Task<ApiResponse<MyResultDto>> GenerateFakeResultAsync(int registrationId)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.PostAsync(
                    $"/api/Races/runner/registrations/{registrationId}/fake-result",
                    null // body rỗng
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<MyResultDto>>();
                    return result ?? new ApiResponse<MyResultDto>
                    {
                        Success = false,
                        Message = "Failed to parse fake result response"
                    };
                }

                // Thử đọc error từ API
                try
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiResponse<MyResultDto>>();
                    if (error != null) return error;
                }
                catch { }

                return new ApiResponse<MyResultDto>
                {
                    Success = false,
                    Message = $"Fake result request failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GenerateFakeResult API");
                return new ApiResponse<MyResultDto>
                {
                    Success = false,
                    Message = "An error occurred while generating fake result",
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



        public async Task<ApiResponse<MyRegistrationDto>> FakePaymentAsync(int registrationId)
        {
            try
            {
                SetAuthorizationHeader();

                var response = await _httpClient.PostAsync(
                    $"/api/Races/runner/registrations/{registrationId}/fake-payment",
                    null // không cần body
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<MyRegistrationDto>>();
                    return result ?? new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Failed to parse fake payment response"
                    };
                }

                // Thử parse error body (API có thể gửi message chuẩn)
                try
                {
                    var error = await response.Content.ReadFromJsonAsync<ApiResponse<MyRegistrationDto>>();
                    if (error != null) return error;
                }
                catch { }

                return new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = $"Fake payment failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling FakePayment API");
                return new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = "An error occurred while calling fake payment API",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// GET: /api/Races/runner/profile
        /// </summary>
        public async Task<ApiResponse<RunnerProfileDto>> GetRunnerProfileAsync()
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync("/api/Races/runner/profile");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<RunnerProfileDto>>();
                    return result ?? new ApiResponse<RunnerProfileDto>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                return new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = $"API call failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetRunnerProfile API");
                return new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching profile",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// PUT: /api/Races/runner/profile
        /// </summary>
        public async Task<ApiResponse<RunnerProfileDto>> UpdateRunnerProfileAsync(UpdateRunnerProfileRequest request)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.PutAsJsonAsync("/api/Races/runner/profile", request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<RunnerProfileDto>>();
                    return result ?? new ApiResponse<RunnerProfileDto>
                    {
                        Success = false,
                        Message = "Failed to parse response"
                    };
                }

                // Try to read error message from response
                try
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<RunnerProfileDto>>();
                    if (errorResponse != null)
                        return errorResponse;
                }
                catch { }

                return new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = $"Update failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UpdateRunnerProfile API");
                return new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = "An error occurred while updating profile",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // ==================== BLOG METHODS ====================

        public async Task<ApiResponse<List<BlogListItemDto>>> GetBlogsAsync(
            string? search = null,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                SetAuthorizationHeader();

                var query = $"?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query += $"&search={Uri.EscapeDataString(search)}";
                }

                var response = await _httpClient.GetAsync($"/api/Blogs{query}");

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<ApiResponse<List<BlogListItemDto>>>();

                    return result ?? new ApiResponse<List<BlogListItemDto>>
                    {
                        Success = false,
                        Message = "Failed to parse blog list response"
                    };
                }

                return new ApiResponse<List<BlogListItemDto>>
                {
                    Success = false,
                    Message = $"Blog list request failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetBlogs API");
                return new ApiResponse<List<BlogListItemDto>>
                {
                    Success = false,
                    Message = "An error occurred while loading blogs",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<BlogDetailDto>> GetBlogAsync(int id)
        {
            try
            {
                SetAuthorizationHeader();

                var response = await _httpClient.GetAsync($"/api/Blogs/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<ApiResponse<BlogDetailDto>>();

                    return result ?? new ApiResponse<BlogDetailDto>
                    {
                        Success = false,
                        Message = "Failed to parse blog detail response"
                    };
                }

                return new ApiResponse<BlogDetailDto>
                {
                    Success = false,
                    Message = $"Blog detail request failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GetBlog API");
                return new ApiResponse<BlogDetailDto>
                {
                    Success = false,
                    Message = "An error occurred while loading blog detail",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<ToggleLikeResponse>> ToggleLikeAsync(int blogId)
        {
            try
            {
                SetAuthorizationHeader();

                var response = await _httpClient.PostAsync($"/api/Blogs/{blogId}/like", null);

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<ApiResponse<ToggleLikeResponse>>();

                    return result ?? new ApiResponse<ToggleLikeResponse>
                    {
                        Success = false,
                        Message = "Failed to parse toggle like response"
                    };
                }

                return new ApiResponse<ToggleLikeResponse>
                {
                    Success = false,
                    Message = $"Toggle like failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ToggleLike API");
                return new ApiResponse<ToggleLikeResponse>
                {
                    Success = false,
                    Message = "An error occurred while toggling like",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<CommentDto>> CreateCommentAsync(int blogId, string content)
        {
            try
            {
                SetAuthorizationHeader();

                var body = new { content };

                var response = await _httpClient.PostAsJsonAsync($"/api/Blogs/{blogId}/comments", body);

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<ApiResponse<CommentDto>>();

                    return result ?? new ApiResponse<CommentDto>
                    {
                        Success = false,
                        Message = "Failed to parse create comment response"
                    };
                }

                return new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = $"Create comment failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CreateComment API");
                return new ApiResponse<CommentDto>
                {
                    Success = false,
                    Message = "An error occurred while creating comment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<ApiResponse<object>> DeleteCommentAsync(int commentId)
        {
            try
            {
                SetAuthorizationHeader();

                var response = await _httpClient.DeleteAsync($"/api/Blogs/comments/{commentId}");

                if (response.IsSuccessStatusCode)
                {
                    var result =
                        await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

                    return result ?? new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Comment deleted"
                    };
                }

                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Delete comment failed with status code: {response.StatusCode}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling DeleteComment API");
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting comment",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

    }
}
