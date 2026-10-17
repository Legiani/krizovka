using System;

namespace Krizovka.Users.Domain.Entities
{
    public class BackofficePermission
    {
        public Guid Id { get; set; }

        public Guid RoleId { get; set; }
        public virtual BackofficeRole? Role { get; set; }

        public required string Module { get; set; } // e.g. "PARKING", "USERS"
        public required string Action { get; set; } // e.g. "READ", "WRITE", "*"

        public required string ScopeType { get; set; } // "SYSTEM", "GLOBAL"
        public required string ScopeValue { get; set; } // e.g. "PID", "*"
    }
}
