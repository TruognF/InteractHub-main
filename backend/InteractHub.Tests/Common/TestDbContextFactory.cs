using InteractHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.Tests.Common;

internal static class TestDbContextFactory
{
    // Khởi tạo một Database ảo lưu thẳng trên RAM (In-Memory) sinh ID ngẫu nhiên để Test siêu tốc.
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
