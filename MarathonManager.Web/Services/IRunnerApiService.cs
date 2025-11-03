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
    }
}
