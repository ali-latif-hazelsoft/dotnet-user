using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_user.Dtos.User;
using dotnet_user.Models;

namespace dotnet_user.Services.UserService
{
    public interface IUserService
    {
        Task<PagedResponse<List<GetUserDto>>> GetAllUsers(UserQueryDto query);
        Task<GetUserDto> GetUserById(int id);
        Task<GetUserDto> AddUser(AddUserDto newUser);
        Task<GetUserDto> UpdateUser(UpdateUserDto updatedUser);
        Task<string> DeleteUser(int id);
    }
}
