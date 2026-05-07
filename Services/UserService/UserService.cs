using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_user.Constants;
using dotnet_user.Data;
using dotnet_user.Dtos.User;
using dotnet_user.Helpers;
using dotnet_user.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_user.Services.UserService
{
    public class UserService : IUserService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;

        public UserService(IMapper mapper, DataContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        private static string NormalizeEmail(string email)
        {
            return email?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        public async Task<PagedResponse<List<GetUserDto>>> GetAllUsers(UserQueryDto query)
        {
            query ??= new UserQueryDto();

            IQueryable<User> usersQuery = _context.Users.AsNoTracking();

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
            User user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<GetUserDto> AddUser(AddUserDto newUser)
        {
            if (newUser == null)
            {
                throw new ArgumentNullException(nameof(newUser), "User data is required.");
            }

            string email = NormalizeEmail(newUser.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.");
            }

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.Email != null && NormalizeEmail(u.Email) == email
            );

            if (emailExists)
            {
                throw new ArgumentException("Email already exists.");
            }

            User user = _mapper.Map<User>(newUser);
            user.Email = newUser.Email.Trim();

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return _mapper.Map<GetUserDto>(user);
        }

        public async Task<GetUserDto> UpdateUser(UpdateUserDto updatedUser)
        {
            if (updatedUser == null)
            {
                throw new ArgumentNullException(nameof(updatedUser), "User data is required.");
            }

            User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == updatedUser.Id);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            string email = NormalizeEmail(updatedUser.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.");
            }

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.Id != updatedUser.Id && u.Email != null && NormalizeEmail(u.Email) == email
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
            User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

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
