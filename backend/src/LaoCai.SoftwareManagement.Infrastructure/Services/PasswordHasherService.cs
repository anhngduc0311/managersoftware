using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using Microsoft.AspNetCore.Identity;

namespace LaoCai.SoftwareManagement.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static readonly User _dummyUser = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(_dummyUser, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword) || string.IsNullOrEmpty(providedPassword))
            return false;

        var result = _hasher.VerifyHashedPassword(_dummyUser, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
