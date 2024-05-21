using System;
using IMSMIS.Models;
using Microsoft.EntityFrameworkCore;

namespace IMSMIS.Data
{
	public class IMSContext : DbContext
	{
		public IMSContext(DbContextOptions<IMSContext> options) : base(options)
		{
		}
		public DbSet<User> Users { get; set; }
	}
}

