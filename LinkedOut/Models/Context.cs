using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace LinkedOut.Models
{
    public class Context :DbContext
    {

        public DbSet<Auth> Auths { get; set; }
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

    }
}
