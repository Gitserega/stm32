using Microsoft.AspNetCore.SignalR;
namespace Diploma.Api.Hubs;

/* Клиенты подписываются на метод "ReceiveMeasurement"
 * Blazor: await hubConnection.On<VibrationDto>("ReceiveMeasurement", dto => ...) */
public class VibrationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}