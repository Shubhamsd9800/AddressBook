using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.Helper;
using BusinessLayer.Service;
using Moq;
using NUnit.Framework;
using RepositoryLayer.DTO;
using RepositoryLayer.Interface;
using RepositoryLayer.Model;

namespace AddressBookApplication.Tests.BusinessLayer
{
    [TestFixture]
    public class UserBLTests
    {
        private Mock<IUserRL> _mockRepository;
        private Mock<JwtHelper> _mockJwtHelper;
        private Mock<RabbitMQPublisher> _mockRabbitMQPublisher;
        private UserBL _userBL;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IUserRL>();
            _mockJwtHelper = new Mock<JwtHelper>();
            _mockRabbitMQPublisher = new Mock<RabbitMQPublisher>();

            _userBL = new UserBL(_mockRepository.Object, _mockJwtHelper.Object, _mockRabbitMQPublisher.Object);
        }

        [Test]
        public async Task RegisterUserAsync_ShouldReturnToken_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var userDto = new UserDto { Name = "John", Email = "john@example.com", Password = "Test@123" };
            _mockRepository.Setup(repo => repo.GetByEmailAsync(userDto.Email)).ReturnsAsync((User)null);
            _mockRepository.Setup(repo => repo.AddUserAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
            _mockJwtHelper.Setup(jwt => jwt.GenerateToken(userDto.Email, It.IsAny<int>(), "User")).Returns("mock_token");

            // Act
            var response = await _userBL.RegisterUserAsync(userDto);

            // Assert
            Assert.IsTrue(response.Success);
            Assert.AreEqual("mock_token", response.Data);
        }
    }
}
