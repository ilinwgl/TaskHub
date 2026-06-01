using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskHub.Domain.Common;
using TaskHub.Domain.ValueObjects;

namespace TaskHub.Domain.Entities
{
    public class User : BaseEntity
    {
        public Email Email { get; set; } = null!;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}