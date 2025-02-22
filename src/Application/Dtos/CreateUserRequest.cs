using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMS.Auth.Application.Dtos
{  
   public record CreateUserRequest
   (
       string Username,
       string Email,
       string AgencyId // which agency/realm to use
   );
}
