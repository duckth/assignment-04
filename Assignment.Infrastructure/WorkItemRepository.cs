namespace Assignment.Infrastructure;

public class WorkItemRepository : IWorkItemRepository, IDisposable
{
    private KanbanContext _context;

    public WorkItemRepository(KanbanContext context)
    {
        _context = context;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
    private static WorkItemDTO WorkItemDTOFromWorkItem(WorkItem task) => new WorkItemDTO
    (
        Id: task.Id,
        Title: task.Title,
        AssignedToName: task.AssignedTo != null ? task.AssignedTo.Name : "",
        Tags: task.Tags.Select(t => t.Name).ToList().AsReadOnly(),
        State: task.State
    );

    private static WorkItemDetailsDTO WorkItemDetailsDTOFromWorkItem(WorkItem task) => new WorkItemDetailsDTO
    (
        Id: task.Id,
        Title: task.Title,
        Description: task.Description != null ? task.Description : "",
        Created: task.Created,
        StateUpdated: task.StateUpdated,
        AssignedToName: task.AssignedTo != null ? task.AssignedTo.Name : "",
        Tags: task.Tags.Select(t => t.Name).ToList().AsReadOnly(),
        State: task.State
    );

    private Tag FindOrCreateTag(string tagName)
    {
        var tagInDB = _context.Tags.Where(t => t.Name.Equals(tagName));
        if (tagInDB.Any()) return tagInDB.First();

        return new Tag(name: tagName);
    }

    public (Response Response, int ItemId) Create(WorkItemCreateDTO item)
    {
        var assignedUser = item.AssignedToId != null ? _context.Users.Find(item.AssignedToId) : null!;
        if (assignedUser == null && item.AssignedToId != null) return (Response.BadRequest, -1);
        var tags = item.Tags.Select(t => FindOrCreateTag(t)).ToHashSet();

        var newWorkItem = new WorkItem(title: item.Title);
        newWorkItem.AssignedTo = assignedUser;
        newWorkItem.Description = item.Description;
        newWorkItem.State = State.New;
        newWorkItem.Created = DateTime.UtcNow;
        newWorkItem.StateUpdated = DateTime.UtcNow;
        newWorkItem.Tags = tags;
        _context.Items.Add(newWorkItem);
        _context.SaveChanges();

        return (Response.Created, newWorkItem.Id);
    }

    public Response Delete(int taskId)
    {
        var task = _context.Items.Find(taskId);
        if (task == null) return Response.NotFound;
        if (new[] { State.Resolved, State.Closed, State.Removed }.Contains(task.State)) return Response.Conflict;
        if (task.State == State.Active)
        {
            task.State = State.Removed;
            _context.SaveChanges();

            return Response.Updated;
        }

        _context.Items.Remove(task);
        _context.SaveChanges();

        return Response.Deleted;
    }

    public WorkItemDetailsDTO Find(int taskId)
    {
        var task = _context.Items.Find(taskId);
        return task != null ? WorkItemDetailsDTOFromWorkItem(task) : null!;

        // find in context.tasks, create DTO with fields
        //maybe
    }

    public IReadOnlyCollection<WorkItemDTO> Read()
    {
        var taskDTOs = _context.Items.Select(task => WorkItemDTOFromWorkItem(task));
        return taskDTOs.ToList().AsReadOnly();
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        var tasksWithState = _context.Items
                            .Where(task => task.State == state)
                            .Select(task => WorkItemDTOFromWorkItem(task));
        return tasksWithState.ToList().AsReadOnly();
        // like above but chain .Where 
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag)
    {
        //find all tasks where the list of tags contain the specified tag

        var taskDTOs = _context.Items.Select(task => WorkItemDTOFromWorkItem(task))
                                     .ToList()
                                     .Where(task => task.Tags.Contains(tag))
                                     .ToList()
                                     .AsReadOnly();

        return taskDTOs;
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        //find all tasks that are assigned to the specified userId
        var byUser = _context.Users.Find(userId);
        if (byUser == null) return new List<WorkItemDTO>().AsReadOnly();

        var taskDTOs = _context.Items.Where(task => byUser.Id == userId)
                                     .Select(task => WorkItemDTOFromWorkItem(task))
                                     .ToList()
                                     .AsReadOnly();

        return taskDTOs;

    }

    public IReadOnlyCollection<WorkItemDTO> ReadRemoved()
    {
        //find all tasks with state = Removed
        var tasksWithStateRemoved = _context.Items
                            .Where(task => task.State == State.Removed)
                            .Select(task => WorkItemDTOFromWorkItem(task));
        return tasksWithStateRemoved.ToList().AsReadOnly(); ;
    }

    public Response Update(WorkItemUpdateDTO task)
    {
        var curWorkItem = _context.Items.Find(task.Id);
        if (curWorkItem == null) return Response.NotFound;

        curWorkItem.Title = task.Title;
        curWorkItem.AssignedTo = _context.Users.Find(task.AssignedToId);
        curWorkItem.Description = task.Description;

        curWorkItem.Tags = task.Tags.Select(name => FindOrCreateTag(name)).ToList();

        if (curWorkItem.State != task.State)
        {
            curWorkItem.State = task.State;
            curWorkItem.StateUpdated = DateTime.UtcNow;
        }

        _context.SaveChanges();
        return Response.Updated;
    }
}

