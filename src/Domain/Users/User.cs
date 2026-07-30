namespace Domain.Users;

public sealed class User
{
    public UserId Id { get; set; }
    public Email Email { get; set; }
    public Username Username { get; set; }
    public Password Password { get; set; }

    // Sign-Up
    public User(Email email, Username username, Password password)
    {
        Id = new UserId(Guid.CreateVersion7());
        Email = email;
        Username = username;
        Password = password;
    }

    #pragma warning disable CS8618
    // Used for Entity Framework Core
    private User() {}
    #pragma warning restore CS8618
}

