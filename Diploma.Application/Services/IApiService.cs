using Diploma.DTO;
using Diploma.DTO.Auth;
using Diploma.DTO.Config;
using Diploma.DTO.History;
using Diploma.DTO.User;

namespace Diploma.Application.Services
{
    public interface IApiService
    {
        Task<LoginResponse?> LoginAsync(string email, string password);
        Task<List<HistoryDataPoint>?> GetHistoryDataAsync(DateTime start, DateTime end, string parameter);
        Task<List<AlertDto>?> GetAlertsAsync(DateTime? from, DateTime? to);
        Task<List<UserDto>?> GetUsersAsync();
        Task<bool> CreateUserAsync(CreateUserRequest request);
        Task<bool> DeleteUserAsync(long userId);
        Task<bool> UpdateUserAsync(long userId, UpdateUserRequest request);
        Task<List<DeviceThresholdDto>?> GetDevicesThresholdsAsync();
        Task<bool> UpdateThresholdsAsync(UpdateThresholdsRequest request);
        Task<bool> CreateDeviceAsync(CreateDeviceRequest request);
    }
}
