using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_user.Data;
using dotnet_user.Dtos.User;
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

        private static IQueryable<T> ApplySorting<T>(
            IQueryable<T> source,
            string sortBy,
            string sortDirection
        )
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyName = string.IsNullOrWhiteSpace(sortBy) ? "Id" : sortBy;

            var property = Expression.PropertyOrField(parameter, propertyName);
            var lambda = Expression.Lambda(property, parameter);

            string methodName =
                sortDirection?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";

            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(T), property.Type },
                source.Expression,
                Expression.Quote(lambda)
            );

            return source.Provider.CreateQuery<T>(resultExpression);
        }

        private static string NormalizeEmail(string email)
        {
            return email?.Trim().ToLowerInvariant() ?? "";
        }

        public async Task<ServiceResponse<PagedResponse<List<GetUserDto>>>> GetAllUsers(
            UserQueryDto query
        )
        {
            ServiceResponse<PagedResponse<List<GetUserDto>>> serviceResponse =
                new ServiceResponse<PagedResponse<List<GetUserDto>>>();

            query ??= new UserQueryDto();

            IQueryable<User> usersQuery = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                string search = query.SearchTerm.Trim().ToLower();

                usersQuery = usersQuery.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(search))
                    || (u.LastName != null && u.LastName.ToLower().Contains(search))
                    || (u.Email != null && u.Email.ToLower().Contains(search))
                );
            }

            usersQuery = ApplySorting(usersQuery, query.SortBy, query.SortDirection);

            int pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            int pageSize = query.PageSize <= 0 ? 10 : query.PageSize;

            int totalRecords = await usersQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRecords / (float)pageSize);

            List<User> users = await usersQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<GetUserDto> mappedUsers = _mapper.Map<List<GetUserDto>>(users);

            serviceResponse.Data = new PagedResponse<List<GetUserDto>>
            {
                Data = mappedUsers,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
            };

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> GetUserById(int id)
        {
            ServiceResponse<GetUserDto> serviceResponse = new ServiceResponse<GetUserDto>();

            User user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found.";
                return serviceResponse;
            }

            serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> AddUser(AddUserDto newUser)
        {
            ServiceResponse<GetUserDto> serviceResponse = new ServiceResponse<GetUserDto>();

            string email = NormalizeEmail(newUser.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Email is required.";
                return serviceResponse;
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);

            if (emailExists)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Email already exists.";
                return serviceResponse;
            }

            User user = _mapper.Map<User>(newUser);
            user.Email = newUser.Email.Trim();

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            serviceResponse.Message = "User created successfully.";

            return serviceResponse;
        }

        public async Task<ServiceResponse<GetUserDto>> UpdateUser(UpdateUserDto updatedUser)
        {
            ServiceResponse<GetUserDto> serviceResponse = new ServiceResponse<GetUserDto>();

            User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == updatedUser.Id);

            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found.";
                return serviceResponse;
            }

            string email = NormalizeEmail(updatedUser.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Email is required.";
                return serviceResponse;
            }

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.Id != updatedUser.Id && u.Email.ToLower() == email
            );

            if (emailExists)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "Email already exists.";
                return serviceResponse;
            }

            _mapper.Map(updatedUser, user);
            user.Email = updatedUser.Email.Trim();

            await _context.SaveChangesAsync();

            serviceResponse.Data = _mapper.Map<GetUserDto>(user);
            serviceResponse.Message = "User updated successfully.";

            return serviceResponse;
        }

        public async Task<ServiceResponse<string>> DeleteUser(int id)
        {
            ServiceResponse<string> serviceResponse = new ServiceResponse<string>();

            User user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                serviceResponse.Success = false;
                serviceResponse.Message = "User not found.";
                return serviceResponse;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            serviceResponse.Data = "User deleted successfully.";
            serviceResponse.Message = "Success";

            return serviceResponse;
        }
    }
}
