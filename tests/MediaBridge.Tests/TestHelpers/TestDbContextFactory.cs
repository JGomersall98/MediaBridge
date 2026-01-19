using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MediaBridge.Tests.TestHelpers
{
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a fully mocked MediaBridgeDbContext for testing purposes.
        /// Uses a simplified approach that works with non-virtual DbSet properties.
        /// </summary>
        /// <returns>Mocked MediaBridgeDbContext</returns>
        public static Mock<MediaBridgeDbContext> CreateMockDbContext()
        {
            var options = new DbContextOptionsBuilder<MediaBridgeDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var mockContext = new Mock<MediaBridgeDbContext>(options) { CallBase = true };

            // Setup SaveChangesAsync to return a successful result
            mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            return mockContext;
        }

        /// <summary>
        /// Creates a mocked DbContext with pre-seeded test data
        /// </summary>
        /// <returns>Mocked MediaBridgeDbContext with test data</returns>
        public static Mock<MediaBridgeDbContext> CreateSeededMockDbContext()
        {
            var mockContext = CreateMockDbContext();

            // The actual context will be created with CallBase = true, so we can add data directly
            var context = mockContext.Object;

            // Add test users
            var testUser = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                Salt = "salt",
                EmailVerified = true,
                IsDeleted = false,
                Created = DateTime.UtcNow.AddDays(-30)
            };

            context.Users.Add(testUser);
            context.SaveChanges();

            return mockContext;
        }
    }
}