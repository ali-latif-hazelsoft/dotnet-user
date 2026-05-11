using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_user.Constants;
using dotnet_user.Dtos.User;
using dotnet_user.Helpers;
using dotnet_user.Models;
using dotnet_user.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace dotnet_user.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            IMapper mapper,
            IGenericRepository<User> userRepository,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetLoggedInUserId()
        {
            var userIdValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            if (!int.TryParse(userIdValue, out int userId))
            {
                throw new UnauthorizedAccessException("No authenticated user found.");
            }

            return userId;
        }

        private IQueryable<User> GetLoggedInUserUsersQuery(bool asNoTracking = true)
        {
            int loggedInUserId = GetLoggedInUserId();

            return _userRepository
                .Query(asNoTracking)
                .Where(u => u.ApplicationUserId == loggedInUserId);
        }

        public async Task<PagedResponse<List<GetUserDto>>> GetAllUsers(UserQueryDto query)
        {
            query ??= new UserQueryDto();

            IQueryable<User> usersQuery = GetLoggedInUserUsersQuery();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                string search = query.SearchTerm.Trim().ToLower();

                usersQuery = usersQuery.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(search))
                    || (u.LastName != null && u.LastName.ToLower().Contains(search))
                    || (u.Email != null && u.Email.Trim().ToLower().Contains(search))
                );
            }

            usersQuery = usersQuery.ApplySorting(query.SortBy, query.SortDirection);

            int pageNumber =
                query.PageNumber <= 0 ? UserConstants.DefaultPageNumber : query.PageNumber;

            int pageSize = query.PageSize <= 0 ? UserConstants.DefaultPageSize : query.PageSize;

            int totalRecords = await usersQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (float)pageSize);

            List<User> users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<GetUserDto> mappedUsers = _mapper.Map<List<GetUserDto>>(users);

            return new PagedResponse<List<GetUserDto>>
            {
                Data = mappedUsers,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
            };
        }

        public async Task<GetUserDto> GetUserById(int id)
        {
            int loggedInUserId = GetLoggedInUserId();

            User user = await _userRepository
                .Query()
                .FirstOrDefaultAsync(u => u.Id == id && u.ApplicationUserId == loggedInUserId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<GetUserDto> AddUser(AddUserDto newUser)
        {
            int loggedInUserId = GetLoggedInUserId();
            string email = newUser.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            bool emailExists = await _userRepository.AnyAsync(u =>
                u.ApplicationUserId == loggedInUserId
                && u.Email != null
                && u.Email.Trim().ToLower() == email
            );

            if (emailExists)
            {
                throw new ArgumentException("Email already exists.");
            }

            User user = _mapper.Map<User>(newUser);
            user.Email = newUser.Email.Trim();
            user.ApplicationUserId = loggedInUserId;

            await _userRepository.AddAsync(user);

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<GetUserDto> UpdateUser(UpdateUserDto updatedUser)
        {
            int loggedInUserId = GetLoggedInUserId();
            string email = updatedUser.Email?.Trim().ToLowerInvariant() ?? string.Empty;

            User user = await _userRepository
                .Query(false)
                .FirstOrDefaultAsync(u =>
                    u.Id == updatedUser.Id && u.ApplicationUserId == loggedInUserId
                );

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            bool emailExists = await _userRepository.AnyAsync(u =>
                u.ApplicationUserId == loggedInUserId
                && u.Id != updatedUser.Id
                && u.Email != null
                && u.Email.Trim().ToLower() == email
            );

            _mapper.Map(updatedUser, user);
            user.Email = updatedUser.Email.Trim();
            user.ApplicationUserId = loggedInUserId;

            _userRepository.Update(user);

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<string> DeleteUser(int id)
        {
            int loggedInUserId = GetLoggedInUserId();

            User user = await _userRepository
                .Query(false)
                .FirstOrDefaultAsync(u => u.Id == id && u.ApplicationUserId == loggedInUserId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            _userRepository.Remove(user);

            return "User deleted successfully.";
        }
    }
}
