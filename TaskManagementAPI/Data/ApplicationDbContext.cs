using Microsoft.EntityFrameworkCore;

namespace TodoApi{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {}
    }
}
