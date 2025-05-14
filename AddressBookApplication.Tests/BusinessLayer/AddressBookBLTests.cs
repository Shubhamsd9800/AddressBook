using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLayer.Helper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;
using RepositoryLayer.Service;

namespace AddressBookApplication.Tests.BusinessLayer
{
    [TestFixture]
    public class AddressBookBLTests
    {
        private Mock<IAddressBookRL> _mockRepository;
        private Mock<IDistributedCache> _mockCache;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<RabbitMQPublisher> _mockRabbitMQPublisher;
        private AddressBookBL _addressBookBL;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IAddressBookRL>();  // ✅ Initialize first
            _mockCache = new Mock<IDistributedCache>();

            // ✅ Mock Configuration with RabbitMQ settings
            var inMemorySettings = new Dictionary<string, string>
            {
                { "RabbitMQ:Host", "localhost" },
                { "RabbitMQ:Username", "guest" },
                { "RabbitMQ:Password", "guest" },
                { "RabbitMQ:Exchange", "test_exchange" }
            };

            _mockConfiguration = new Mock<IConfiguration>();
            foreach (var setting in inMemorySettings)
            {
                _mockConfiguration.Setup(x => x[setting.Key]).Returns(setting.Value);
            }

            // ✅ Use a real constructor
            _mockRabbitMQPublisher = new Mock<RabbitMQPublisher>(_mockConfiguration.Object) { CallBase = true };

            // ✅ Move setup after initialization
            _mockRepository.Setup(repo => repo.GetAllAsync(It.IsAny<int?>()))
                .ReturnsAsync(new List<AddressBookEntry>
                {
                    new AddressBookEntry { Name = "Alice", Email = "alice@example.com" },
                    new AddressBookEntry { Name = "Bob", Email = "bob@example.com" }
                });

            _addressBookBL = new AddressBookBL(_mockRepository.Object, _mockCache.Object, _mockRabbitMQPublisher.Object);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnContacts()
        {
            // Act
            var result = await _addressBookBL.GetAllAsync(1);

            // Debugging Output
            Console.WriteLine($"🔹 Result: {JsonConvert.SerializeObject(result)}");

            // Assertions
            Assert.IsNotNull(result, "Result should not be null");
            Assert.IsInstanceOf<List<AddressBookEntry>>(result, "Result should be a list of AddressBookEntry");
            Assert.AreEqual(2, result.Count(), "Expected 2 contacts, but got a different count");
        }
    }
}
