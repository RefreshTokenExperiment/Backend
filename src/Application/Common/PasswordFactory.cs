using Application.Abstractions;
using Domain.Users;
using Shared;

namespace Application.Common;

public sealed class PasswordFactory(IPasswordHasher hasher)
{
    public Result<Password> Create(string password)
    {
        var error = Password.Validate(password);
        if (error is not null) return error;

        var hashedPassword = hasher.Hash(password);
        return Password.FromHash(hashedPassword);
    }
}