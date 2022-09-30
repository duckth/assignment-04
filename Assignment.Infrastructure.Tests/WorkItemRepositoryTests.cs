namespace Assignment.Infrastructure.Tests;


[Collection("Sequential")]
public class WorkItemRepositoryTests : IDisposable
{
    private KanbanContext _context;

    private WorkItemRepository _repo;

    public WorkItemRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>();
        builder.UseSqlite(connection);

        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();
        _context = context;
        _repo = new WorkItemRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _repo.Dispose();
    }

    [Fact]
    public void Delete_a_task_with_state_new_return_deleted()
    {
        var task = new WorkItem(title: "testTask");

        _context.Items.Add(task);
        _context.SaveChanges();

        var expected = Response.Deleted;

        var actual = _repo.Delete(task.Id);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Delete_a_task_with_state_active_should_set_state_to_removed()
    {
        var task = new WorkItem(title: "testTask");
        task.State = State.Active;
        _context.Items.Add(task);
        _context.SaveChanges();

        var expectedState = State.Removed;

        var actual = _repo.Delete(task.Id);

        _context.Items.Find(task.Id)!.State.Should().Be(expectedState);
    }

    [Fact]
    public void Delete_a_task_with_state_resolved_closed_removed_returns_conflict()
    {
        var task = new WorkItem(title: "testTask");
        task.State = State.Resolved;
        var task1 = new WorkItem(title: "testTask");
        task1.State = State.Closed;
        var task2 = new WorkItem(title: "testTask");
        task2.State = State.Removed;

        _context.Items.Add(task);
        _context.Items.Add(task1);
        _context.Items.Add(task2);
        _context.SaveChanges();

        var expected = Response.Conflict;

        var actualTask = _repo.Delete(task.Id);
        var actualTask1 = _repo.Delete(task1.Id);
        var actualTask2 = _repo.Delete(task2.Id);

        actualTask.Should().Be(expected);
        actualTask1.Should().Be(expected);
        actualTask2.Should().Be(expected);
    }


    [Fact]
    public void Create_should_set_state_of_new_task_to_New()
    {
        var taskDTO = new WorkItemCreateDTO("testTask", null, null, new HashSet<string>());
        var expected = State.New;

        _repo.Create(taskDTO);

        var actual = _context.Items.OrderBy(t => t.Id).Last().State;

        actual.Should().Be(expected);
    }


    [Fact]
    public void Create_should_set_created_and_stateupdated_to_currenttime()
    {
        var taskDTO = new WorkItemCreateDTO("testTask", null, null, new HashSet<string>());

        _repo.Create(taskDTO);

        var expected = DateTime.UtcNow;
        var actual = _context.Items.OrderBy(t => t.Id).Last().Created;
        var actual1 = _context.Items.OrderBy(t => t.Id).Last().StateUpdated;

        actual.Should().BeCloseTo(expected, precision: TimeSpan.FromSeconds(5));
        actual1.Should().BeCloseTo(expected, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_allows_to_edit_tags()
    {
        var task = new WorkItem(title: "testTask");
        _context.Items.Add(task);
        _context.SaveChanges();

        var taskUpdateDto = new WorkItemUpdateDTO(task.Id, "tesdtUpdateDTO", null, null, new List<string> { "test", "tester" }, State.New);

        var expected = new List<string> { "test", "tester" };

        _repo.Update(taskUpdateDto);

        var actual = _context.Items.Find(task.Id).Tags.Select(t => t.Name);

        actual.Should().BeEquivalentTo(expected);
    }

    //maybe do the same for create if time (above)

    [Fact]
    public void Update_changes_stateupdated_to_currenttime()
    {
        var task = new WorkItem(title: "testTask");
        _context.Items.Add(task);
        _context.SaveChanges();

        var taskUpdateDto = new WorkItemUpdateDTO(task.Id, "tesdtUpdateDTO", null, null, new List<string> { "test", "tester" }, State.Active);

        var expected = DateTime.UtcNow;

        _repo.Update(taskUpdateDto);

        var actual = _context.Items.Find(task.Id).StateUpdated;

        actual.Should().BeCloseTo(expected, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_returns_badrequest_if_assignto_user_dows_not_exist()
    {
        var taskDTO = new WorkItemCreateDTO("testTask", -1, null, new HashSet<string>());

        var expected = (Response.BadRequest, -1);

        var actual = _repo.Create(taskDTO);

        actual.Should().Be(expected);

    }

    [Fact]
    public void ReadAll_should_return_all_elements()
    {
        var task = new WorkItem(title: "testTitle");
        _context.Items.Add(task);
        _context.SaveChanges();

        var tasksInContext = _context.Items.ToList();

        var tasksFromReadAll = _repo.Read();

        var expectedValues = tasksInContext.Select(t => t.Title);
        var actualValues = tasksFromReadAll.Select(t => t.Title);

        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Fact]
    public void ReadAllByTag_should_return_all_elements_with_given_tag()
    {
        var testTag1 = new Tag(name: "tag1");
        var testTag2 = new Tag(name: "tag2");
        var testTag3 = new Tag(name: "tag3");

        var item1 = new WorkItem(title: "testTitle");
        var item2 = new WorkItem(title: "testTitle2");
        var item3 = new WorkItem(title: "testTitle3");
        item1.Tags.Add(testTag1);
        item1.Tags.Add(testTag3);
        item2.Tags.Add(testTag2);
        item2.Tags.Add(testTag3);
        item3.Tags.Add(testTag1);
        item3.Tags.Add(testTag2);

        _context.Tags.AddRange(testTag1, testTag2, testTag3);
        _context.Items.AddRange(item1, item2, item3);
        _context.SaveChanges();

        var itemIdsWithTag1 = new[] { item1.Id, item3.Id };
        var itemIdsWithTag2 = new[] { item2.Id, item3.Id };
        var itemIdsWithTag3 = new[] { item1.Id, item2.Id };

        var actual1 = _repo.ReadByTag("tag1").ToList().Select(t => t.Id);
        var actual2 = _repo.ReadByTag("tag2").ToList().Select(t => t.Id);
        var actual3 = _repo.ReadByTag("tag3").ToList().Select(t => t.Id);

        actual1.Should().BeEquivalentTo(itemIdsWithTag1);
        actual2.Should().BeEquivalentTo(itemIdsWithTag2);
        actual3.Should().BeEquivalentTo(itemIdsWithTag3);
    }

    [Fact]
    public void Read_should_return_TaskDetailsDTO_from_taskId()
    {
        var task = new WorkItem(title: "tester");

        _context.Items.Add(task);
        _context.SaveChanges();

        var actual = _repo.Find(task.Id);
        var expected = new WorkItemDetailsDTO(task.Id, "tester", "", new DateTime(0001, 01, 01), "", new HashSet<string>(), State.New, new DateTime(0001, 01, 01));

        actual.Should().BeEquivalentTo(expected);
    }
}

