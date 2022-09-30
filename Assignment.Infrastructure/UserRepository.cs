namespace Assignment.Infrastructure;

public class UserRepository : IUserRepository, IDisposable
{
    private KanbanContext _context;

    public UserRepository(KanbanContext context)
    {
        _context = context;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public (Response Response, int UserId) Create(UserCreateDTO user)
    {
        var conflicts = _context.Users.Where(u => u.Email.Equals(user.Email)).Select(u => u.Id);

        if (conflicts.Any())
        {
            return (Response.Conflict, conflicts.First());
        }

        var newUser = new User(name: user.Name, email: user.Email);
        _context.Users.Add(newUser);
        _context.SaveChanges();

        return (Response.Created, newUser.Id);
    }

    public Response Delete(int userId, bool force = false)
    {
        var user = _context.Users.Find(userId);

        if (user == null) return Response.NotFound;
        if (user.Items.Count > 0 && !force) return Response.Conflict;
        _context.Users.Remove(user);
        _context.SaveChanges();

        return Response.Deleted;
    }

    public UserDTO Find(int userId)
    {
        var user = _context.Users.Find(userId);
        return user != null ? new UserDTO(userId, user.Name, user.Email) : null!;
    }

    public IReadOnlyCollection<UserDTO> Read()
    {
        var users = _context.Users
                            .Select(user => new UserDTO(user.Id, user.Name, user.Email))
                            .ToList()
                            .AsReadOnly();

        return users;
    }

    public Response Update(UserUpdateDTO user)
    {
        var toUpdate = _context.Users.Find(user.Id);
        if (toUpdate == null) return Response.NotFound;

        if (toUpdate.Name != user.Name) toUpdate.Name = user.Name;
        if (toUpdate.Email != user.Email) toUpdate.Email = user.Email;

        _context.SaveChanges();

        return Response.Updated;
    }
}

