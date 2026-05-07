using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Models;

namespace dotnet_user.Data
{
    public interface IAuthRepository
    {
        Task<ServiceResponse<int>> Register(Member member, string password);
        Task<ServiceResponse<string>> Login(string username, string password);
        Task<bool> MemberExists(string username);
    }
}
