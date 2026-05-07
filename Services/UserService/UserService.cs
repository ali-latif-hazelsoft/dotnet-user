using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_user.Constants;
using dotnet_user.Data;
using dotnet_user.Dtos.User;
using dotnet_user.Helpers;
using dotnet_user.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace dotnet_user.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            IMapper mapper,
            DataContext context,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _mapper = mapper;
            _context = context;
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

        private IQueryable<User> GetLoggedInUserUsersQuery()
        {
            int loggedInUserId = GetLoggedInUserId();
            return _context.Users.AsNoTracking().Where(u => u.ApplicationUserId == loggedInUserId);
        }

        private static string NormalizeEmail(string email)
        {
            return email?.Trim().ToLowerInvariant() ?? string.Empty;
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
                    || (u.Email != null && NormalizeEmail(u.Email).Contains(search))
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

            User user = await _context
                .Users.AsNoTracking()
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

            string email = NormalizeEmail(newUser.Email);

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.ApplicationUserId == loggedInUserId
                && u.Email != null
                && u.Email.ToLower() == email
            );

            if (emailExists)
            {
                throw new ArgumentException("Email already exists.");
            }

            User user = _mapper.Map<User>(newUser);
            user.Email = newUser.Email.Trim();
            user.ApplicationUserId = loggedInUserId;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<GetUserDto> UpdateUser(UpdateUserDto updatedUser)
        {
            int loggedInUserId = GetLoggedInUserId();

            User user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == updatedUser.Id && u.ApplicationUserId == loggedInUserId
            );

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            string email = NormalizeEmail(updatedUser.Email);

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.ApplicationUserId == loggedInUserId
                && u.Id != updatedUser.Id
                && u.Email != null
                && NormalizeEmail(u.Email) == email
            );

            if (emailExists)
            {
                throw new ArgumentException("Email already exists.");
            }

            _mapper.Map(updatedUser, user);
            user.Email = updatedUser.Email.Trim();

            await _context.SaveChangesAsync();

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<string> DeleteUser(int id)
        {
            int loggedInUserId = GetLoggedInUserId();

            User user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == id && u.ApplicationUserId == loggedInUserId
            );

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return "User deleted successfully.";
        }
    }
}
