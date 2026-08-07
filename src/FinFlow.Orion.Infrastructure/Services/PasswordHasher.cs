using System;
using System.Collections.Generic;
using System.Text;

using FinFlow.Orion.Application.Common.Interfaces;
using BCrypt.Net;

namespace FinFlow.Orion.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
