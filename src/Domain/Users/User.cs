namespace Domain.Users;

public sealed class User
{
    public Email Email { get; set; }
    public Username Username { get; set; }
    public Password Password { get; set; }

    // Sign-Up
    public User(Email email, Username username, Password password)
    {
        Email = email;
        Username = username;
        Password = password;
    }
}

