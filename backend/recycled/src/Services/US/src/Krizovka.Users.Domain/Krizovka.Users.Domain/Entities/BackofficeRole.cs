using System;
using System.Collections.Generic;

namespace Krizovka.Users.Domain.Entities
{
    public class BackofficeRole
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public string? Description { get; set; }

        public ICollection<BackofficePermission> Permissions { get; set; } = new List<BackofficePermission>();
        public ICollection<BackofficeUser> Users { get; set; } = new List<BackofficeUser>();
    }
}
