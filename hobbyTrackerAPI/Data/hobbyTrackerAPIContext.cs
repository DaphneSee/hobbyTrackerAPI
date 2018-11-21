using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace hobbyTrackerAPI.Models
{
    public class hobbyTrackerAPIContext : DbContext
    {
        public hobbyTrackerAPIContext (DbContextOptions<hobbyTrackerAPIContext> options)
            : base(options)
        {
        }

        public DbSet<hobbyTrackerAPI.Models.HobbyItem> HobbyItem { get; set; }
    }
}
