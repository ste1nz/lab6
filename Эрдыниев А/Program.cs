using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>();
var app = builder.Build();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    if (!context.Users.Any())
    {
        context.Users.Add(new User { Email = "admin@test.com", Password = "123", Phone = "123" });
        context.Users.Add(new User { Email = "user@test.com", Password = "123", Phone = "456" });
        context.SaveChanges();
    }
}

app.Run();

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "assigned";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public List<User> Assignees { get; set; } = new();
}

public class AssignmentMessage
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AssignmentId { get; set; }
    public int AuthorId { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<AssignmentMessage> Messages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=app.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>()
            .HasMany(a => a.Assignees)
            .WithMany();
    }
}

[ApiController]
[Route("api/[controller]")]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _context;
    public AssignmentsController(AppDbContext context) => _context = context;

    [HttpGet]
    public List<Assignment> GetAssignments()
        => _context.Assignments.Include(a => a.Author).Include(a => a.Assignees).ToList();

    [HttpPost]
    public Assignment CreateAssignment(Assignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        _context.Assignments.Add(assignment);
        _context.SaveChanges();
        return assignment;
    }

    [HttpPost("{id}/status")]
    public IActionResult ChangeStatus(int id, [FromBody] string status)
    {
        var assignment = _context.Assignments.Find(id);
        if (assignment == null) return NotFound();
        assignment.Status = status;
        _context.SaveChanges();
        return Ok();
    }

    [HttpPost("{id}/messages")]
    public AssignmentMessage AddMessage(int id, AssignmentMessage message)
    {
        message.AssignmentId = id;
        message.CreatedAt = DateTime.UtcNow;
        _context.Messages.Add(message);
        _context.SaveChanges();
        return message;
    }

    [HttpGet("{id}/messages")]
    public List<AssignmentMessage> GetMessages(int id)
        => _context.Messages.Where(m => m.AssignmentId == id).ToList();
}