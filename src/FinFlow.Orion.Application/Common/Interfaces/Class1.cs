using System;
using System.Collections.Generic;
using System.Text;

namespace FinFlow.Orion.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
