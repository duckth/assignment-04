namespace Assignment.Infrastructure.Tests;

public class TagRepositoryTests : IDisposable
{

    private KanbanContext _context;

    private TagRepository _repo;


    public TagRepositoryTests()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>();
        builder.UseSqlite(connection);

        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();
        _context = context;
        _repo = new TagRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _repo.Dispose();
    }

    [Fact]
    public void Create_Tag_returns_Created_with_TagId()
    {
        var (response, created) = _repo.Create(new TagCreateDTO("A tag"));

        response.Should().Be(Response.Created);

        created.Should().Be(1);
    }

    [Fact]
    public void Create_when_valid_should_return_created()
    {
        // Arrange
        var newTagDTO = new TagCreateDTO(Name: "testing");
        var expected = Response.Created;
        // Act
        var (response, tagId) = _repo.Create(newTagDTO);

        // Assert
        response.Should().Be(expected);
    }

    [Fact]
    public void Create_when_tag_already_exists_returns_conflict()
    {
        // Arrange
        var conflictingTag = new Tag(name: "testing");
        _context.Tags.Add(conflictingTag);
        _context.SaveChanges();
        var expected = (Response.Conflict, conflictingTag.Id);

        // Act
        var newTagDTO = new TagCreateDTO(Name: "testing");
        var actual = _repo.Create(newTagDTO);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Delete_with_no_force_when_tag_assigned_returns_conflict()
    {
        var blockingTask = new WorkItem(title: "Blocker!");
        var tag = new Tag(name: "testing");
        tag.WorkItems.Add(blockingTask);
        _context.Tags.Add(tag);
        _context.Items.Add(blockingTask);
        _context.SaveChanges();
        var expected = Response.Conflict;

        var actual = _repo.Delete(tag.Id);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Delete_with_the_force_deletes_if_assigned()
    {
        var blockingTask = new WorkItem(title: "Blocker!");
        var tag = new Tag(name: "testing");

        _context.Tags.Add(tag);
        _context.Items.Add(blockingTask);
        _context.SaveChanges();
        var expected = Response.Deleted;

        var actual = _repo.Delete(tagId: tag.Id, force: true);

        actual.Should().Be(expected);
    }

    [Fact]
    public void Update_updates_information_and_returns_updated()
    {
        var tag = new Tag(name: "test");
        _context.Tags.Add(tag);
        _context.SaveChanges();
        var expected = Response.Updated;

        var actual = _repo.Update(new TagUpdateDTO(tag.Id, "updated_tag"));

        actual.Should().Be(expected);
    }

    [Fact]
    public void Update_updates_tag_to_have_new_information_correct()
    {
        var tag = new Tag(name: "testing");
        _context.Tags.Add(tag);
        _context.SaveChanges();

        var expectedName = "updated_tag";

        var actual = _repo.Update(new TagUpdateDTO(tag.Id, "updated_tag"));

        _context.Tags.Find(tag.Id).Name.Should().Be(expectedName);
    }


    [Fact]
    public void Read_if_no_tag_found_returns_null()
    {
        var actual = _repo.Find(Int32.MaxValue);
        actual.Should().Be(null);
    }

    [Fact]
    public void Read_returns_correct_info_if_user_found()
    {

        var tag = new Tag(name: "testTag");
        _context.Tags.Add(tag);
        _context.SaveChanges();

        var expected = new TagDTO(tag.Id, "testTag");

        var actual = _repo.Find(tag.Id);

        actual.Should().Be(expected);
    }



    [Fact]
    public void ReadAll_should_return_all_elements()
    {

        var tag1 = new Tag(name: "testing");
        var tag2 = new Tag(name: "testing2");
        var tag3 = new Tag(name: "testing3");
        _context.Tags.AddRange(new[] { tag1, tag2, tag3 });
        _context.SaveChanges();

        var tagsInContext = _context.Tags.ToList();

        var tagsFromReadAll = _repo.Read();

        var expectedValues = tagsInContext.Select(tag => (tag.Id, tag.Name));
        var actualValues = tagsFromReadAll.Select(dto => (dto.Id, dto.Name));

        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Fact]
    public void Update_should_return_notFound_if_user_not_found()
    {
        var tagDTO = new TagUpdateDTO(Int32.MaxValue, "updated_tag");

        var expected = Response.NotFound;

        var actual = _repo.Update(tagDTO);

        actual.Should().Be(expected);
    }
}
