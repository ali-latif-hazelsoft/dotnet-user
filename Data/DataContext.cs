using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_user.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Member> Members { get; set; }
    }
}
