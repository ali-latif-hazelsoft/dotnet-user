using System.Threading.Tasks;
using dotnet_user.Dtos.ApplicationUser;
using dotnet_user.Dtos.Auth;

namespace dotnet_user.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto request);
        Task<InviteUserResponseDto> InviteUserAsync(InviteUserDto request);
        Task<ConfirmEmailResponseDto> ConfirmEmailAndSetPasswordAsync(SetPasswordDto request);
    }
}
