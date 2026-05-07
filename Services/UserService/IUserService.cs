using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Dtos.User;
using dotnet_user.Models;

namespace dotnet_user.Services.UserService
{
    public interface IUserService
    {
        Task<ServiceResponse<PagedResponse<List<GetUserDto>>>> GetAllUsers(UserQueryDto query);
        Task<ServiceResponse<GetUserDto>> GetUserById(int id);
        Task<ServiceResponse<GetUserDto>> AddUser(AddUserDto newUser);
        Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser);
        Task<ServiceResponse<string>> DeleteUser(int id);
    }
}
