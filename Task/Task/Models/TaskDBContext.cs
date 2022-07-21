using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task.Models;

namespace Task.Models
{
	public class TaskDBContext : DbContext
	{
        public TaskDBContext(DbContextOptions<TaskDBContext> options)
            : base(options)
        {
        }
        public DbSet<TaskItem> Tasks { get; set; }
    }
}

