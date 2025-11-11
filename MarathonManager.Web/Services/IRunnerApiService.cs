using MarathonManager.API.DTOs;

namespace MarathonManager.Web.Services
{
    public interface IRunnerApiService
    {
        Task<ApiResponse<RunnerDashboardDto>> GetDashboardAsync();
        Task<ApiResponse<PaginatedResponse<AvailableRaceDto>>> GetAvailableRacesAsync(int pageNumber = 1, int pageSize = 6);
        Task<ApiResponse<RaceDetailsDto>> GetRaceDetailsAsync(int raceId);
        Task<ApiResponse<MyRegistrationDto>> RegisterForRaceAsync(RegisterForRaceRequest request);
        Task<ApiResponse<PaginatedResponse<MyRegistrationDto>>> GetMyRegistrationsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<object>> CancelRegistrationAsync(int registrationId);
        Task<ApiResponse<PaginatedResponse<MyResultDto>>> GetMyResultsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponse<MyRegistrationDto>> FakePaymentAsync(int registrationId);
        Task<ApiResponse<RunnerProfileDto>> GetRunnerProfileAsync();
        Task<ApiResponse<RunnerProfileDto>> UpdateRunnerProfileAsync(UpdateRunnerProfileRequest request);
        Task<ApiResponse<MyResultDto>> GenerateFakeResultAsync(int registrationId);
        // Services/IRunnerApiService.cs
        Task<ApiResponse<List<BlogListItemDto>>> GetBlogsAsync(string? search = null, int page = 1, int pageSize = 10);
        Task<ApiResponse<BlogDetailDto>> GetBlogAsync(int id);
        Task<ApiResponse<ToggleLikeResponse>> ToggleLikeAsync(int blogId);
        Task<ApiResponse<CommentDto>> CreateCommentAsync(int blogId, string content);
        Task<ApiResponse<object>> DeleteCommentAsync(int commentId);

    }

}
