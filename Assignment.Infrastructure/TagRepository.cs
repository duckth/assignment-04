namespace Assignment.Infrastructure;


public class TagRepository : ITagRepository, IDisposable
{
    private KanbanContext _context;

    public TagRepository(KanbanContext context)
    {
        _context = context;
    }

    public void Dispose()
    {
        // do Stuff
        _context.Dispose();
    }

    public (Response Response, int TagId) Create(TagCreateDTO tag)
    {
        var conflicts = _context.Tags.Where(t => t.Name == tag.Name).Select(t => t.Id);

        if (conflicts.Any())
        {
            return (Response.Conflict, conflicts.First());
        }

        var newTag = new Tag(name: tag.Name);
        _context.Tags.Add(newTag);
        _context.SaveChanges();

        return (Response.Created, newTag.Id);
    }

    public Response Delete(int tagId, bool force = false)
    {
        var tag = _context.Tags.Find(tagId);

        if (tag == null) return Response.NotFound;
        if (tag.WorkItems.Count > 0 && !force) return Response.Conflict;

        _context.Tags.Remove(tag);
        _context.SaveChanges();

        return Response.Deleted;
    }

    public TagDTO Find(int tagId)
    {
        var tag = _context.Tags.Find(tagId);
        return tag != null ? new TagDTO(tag.Id, tag.Name) : null!;
    }

    public IReadOnlyCollection<TagDTO> Read()
    {
        var tags = _context.Tags
                            .Select(tag => new TagDTO(tag.Id, tag.Name))
                            .ToList()
                            .AsReadOnly();

        return tags;
    }

    public Response Update(TagUpdateDTO tag)
    {
        var toUpdate = _context.Tags.Find(tag.Id);
        if (toUpdate == null) return Response.NotFound;

        toUpdate.Name = tag.Name;
        _context.SaveChanges();

        return Response.Updated;
    }
}

