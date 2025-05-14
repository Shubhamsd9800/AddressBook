using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using RepositoryLayer.Context;
using RepositoryLayer.Model;
using RepositoryLayer.Service;

namespace AddressBookApplication.Tests.RepositoryLayer
{
    [TestFixture]
    public class AddressBookRLTests
    {
        private AppDbContext _context;
        private UserRL _userRepository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase") // ✅ Fix: Use In-Memory Database
                .Options;

            _context = new AppDbContext(options);
            _userRepository = new UserRL(_context);
        }

        [Test]
        public async Task AddUserAsync_ShouldAddUserToDatabase()
        {
            // Arrange
            var user = new User { Name = "Alice", Email = "alice@example.com", PasswordHash = "hashedpassword" };

            // Act
            await _userRepository.AddUserAsync(user);
            var result = await _context.Users.FirstOrDefaultAsync(u => u.Email == "alice@example.com");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Alice", result.Name);
        }

        [TearDown]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
