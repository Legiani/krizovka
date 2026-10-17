using System;

namespace Krizovka.Users.Domain.Entities
{
    public class BackofficeUser
    {
        public Guid Id { get; set; }
        public Guid? IdentityId { get; set; } // Map to Kratos Identity (now optional)
        public Guid? AccountId { get; set; } // Map to UserAccount (Customer)
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsSuperAdmin { get; set; }
        public bool IsActive { get; set; }
        public string PasswordHash { get; set; } = string.Empty;

        public Guid? RoleId { get; set; }
        public BackofficeRole? Role { get; set; }
    }
}
