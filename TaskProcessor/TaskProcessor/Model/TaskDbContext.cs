using TaskProcessor.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;



namespace TaskProcessor.Model
{
	public class TaskDbContext: DbContext
	{
        public TaskDbContext (DbContextOptions<TaskDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> Task { get; set; }
    }
}

