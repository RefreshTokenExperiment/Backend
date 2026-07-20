namespace Domain.Users;

public sealed class User
{
    public Email Email { get; init; }
    public Username Username { get; init; }
    public Password Password { get; init; }

    // Sign-Up
    public User(Email email, Username username, Password password)
    {
        Email = email;
        Username = username;
        Password = password;
    }
}

