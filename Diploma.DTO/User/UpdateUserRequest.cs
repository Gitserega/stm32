using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.DTO.User
{
    public class UpdateUserRequest
    {
        public string? Login { get; set; }
        public string? Password { get; set; }
        public bool? IsAdmin { get; set; }
    }
}
