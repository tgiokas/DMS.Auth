using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Application.Dtos
{
    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
