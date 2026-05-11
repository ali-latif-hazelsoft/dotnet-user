using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_user.Helpers
{
    public static class ValidationHelpers
    {
        public static bool IsValidId(int id)
        {
            return id > 0;
        }

        public static string NormalizeString(string str)
        {
            return str.Trim().ToLowerInvariant();
        }
    }
}
