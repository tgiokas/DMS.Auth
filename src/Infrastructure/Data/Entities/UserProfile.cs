using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Auth.Infrastructure.Data.Entities
{
    public class UserProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Username { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        // Additional fields
    }
}
