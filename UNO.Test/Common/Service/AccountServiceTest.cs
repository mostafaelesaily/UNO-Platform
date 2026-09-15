using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using AutoMapper;

using UNO.Application.Services.Common;
using UNO.Domain.Entities.Common.Identity;
using UNO.Application.DTOs.Common.identity;

namespace UNO.Test.Common.Service
{
    public class AccountServiceTest
    {
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<ILogger<AccountService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IMapper> _mapperMock;

        private readonly AccountService _accountService;

        public AccountServiceTest()
        {
            _userManagerMock = new Mock<UserManager<AppUser>>(
                Mock.Of<IUserStore<AppUser>>(),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );

            _loggerMock = new Mock<ILogger<AccountService>>();
            _configurationMock = new Mock<IConfiguration>();
            _mapperMock = new Mock<IMapper>();

            // JWT configuration
            _configurationMock
                .Setup(x => x["JWT:Key"])
                .Returns("this-is-a-test-secret-key-123456789");

            _configurationMock
                .Setup(x => x["JWT:Issuer"])
                .Returns("TestIssuer");

            _configurationMock
                .Setup(x => x["JWT:Audience"])
                .Returns("TestAudience");

            _accountService = new AccountService(
                _userManagerMock.Object,
                _loggerMock.Object,
                _configurationMock.Object,
                _mapperMock.Object
            );
        }


        [Fact]
        public async Task GenerateAccessToken_Should_Return_Valid_Token()
        {
            // Arrange

            var user = new AppUser
            {
                Id = "user-123",
                Email = "test@test.com",
                UserName = "testuser"
            };

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            // Act

            var token = await _accountService.GenrateAccessToken(user);

            // Assert

            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);

            Assert.NotNull(jwtToken);
        }


        [Fact]
        public async Task GenerateAccessToken_Should_Contain_User_Claims()
        {
            // Arrange

            var user = new AppUser
            {
                Id = "user-123",
                Email = "test@test.com",
                UserName = "testuser"
            };

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            // Act

            var token = await _accountService.GenrateAccessToken(user);

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);

            // Assert

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == ClaimTypes.NameIdentifier &&
                    claim.Value == user.Id
            );

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == ClaimTypes.Email &&
                    claim.Value == user.Email
            );

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == ClaimTypes.Name &&
                    claim.Value == user.UserName
            );

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == JwtRegisteredClaimNames.Jti
            );
        }


        [Fact]
        public async Task GenerateAccessToken_Should_Contain_User_Roles()
        {
            // Arrange

            var user = new AppUser
            {
                Id = "user-123",
                Email = "test@test.com",
                UserName = "testuser"
            };

            var roles = new List<string>
            {
                "Admin",
                "Manager"
            };

            _userManagerMock
                .Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act

            var token = await _accountService.GenrateAccessToken(user);

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);

            // Assert

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == ClaimTypes.Role &&
                    claim.Value == "Admin"
            );

            Assert.Contains(
                jwtToken.Claims,
                claim =>
                    claim.Type == ClaimTypes.Role &&
                    claim.Value == "Manager"
            );
        }
    

    [Fact]
        public async Task GenrateRefreshToken_Should_Return_Valid_Token()
        {
            // Act
            var result = await _accountService.GenrateRefreshToken();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotEmpty(result.Token);
            Assert.True(result.ExpiresOn > DateTime.UtcNow);
        }

        [Fact]
        public async Task HandleRefreshTokenAsync_Should_Throw_ArgumentNullException_When_Dto_Is_Null()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _accountService.HandleRefreshTokenAsync(null));
        }

        [Fact]
        public async Task HandleRefreshTokenAsync_Should_Throw_UnauthorizedException_When_Token_Not_Found()
        {
            // Arrange
            var refreshTokenDto = new GenrateRefreshToken { Token = "invalid-token" };
            var users = new List<AppUser>().AsQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UNO.Application.Exceptions.UnauthorizedException>(() =>
                _accountService.HandleRefreshTokenAsync(refreshTokenDto));
            Assert.Equal("Invalid Refresh Token", exception.Message);
        }

        [Fact]
        public async Task HandleRefreshTokenAsync_Should_Throw_UnauthorizedException_When_Token_Expired()
        {
            // Arrange
            var tokenValue = "expired-token";
            var refreshTokenDto = new GenrateRefreshToken { Token = tokenValue };
            var user = new AppUser
            {
                RefreshTokens = new List<RefreshTokens>
            {
                new RefreshTokens { Token = tokenValue, ExpiresOn = DateTime.UtcNow.AddDays(-1) }
            }
            };
            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UNO.Application.Exceptions.UnauthorizedException>(() =>
                _accountService.HandleRefreshTokenAsync(refreshTokenDto));
            Assert.Equal("Invalid or Expired Refresh Token", exception.Message);
        }

        [Fact]
        public async Task HandleRefreshTokenAsync_Should_Throw_UnauthorizedException_When_Token_Revoked()
        {
            // Arrange
            var tokenValue = "revoked-token";
            var refreshTokenDto = new GenrateRefreshToken { Token = tokenValue };
            var user = new AppUser
            {
                RefreshTokens = new List<RefreshTokens>
            {
                new RefreshTokens { Token = tokenValue, ExpiresOn = DateTime.UtcNow.AddDays(1), revokedOn = DateTime.UtcNow }
            }
            };
            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UNO.Application.Exceptions.UnauthorizedException>(() =>
                _accountService.HandleRefreshTokenAsync(refreshTokenDto));
            Assert.Equal("Invalid or Expired Refresh Token", exception.Message);
        }

        [Fact]
        public async Task HandleRefreshTokenAsync_Should_Return_Valid_Response_When_Token_Is_Valid()
        {
            // Arrange
            var tokenValue = "valid-token";
            var refreshTokenDto = new GenrateRefreshToken { Token = tokenValue };
            var user = new AppUser
            {
                Id = "user-1",
                Email = "test@test.com",
                UserName = "testuser",
                RefreshTokens = new List<RefreshTokens>
            {
                new RefreshTokens { Token = tokenValue, ExpiresOn = DateTime.UtcNow.AddDays(1), revokedOn = null }
            }
            };
            var users = new List<AppUser> { user }.AsQueryable();
            _userManagerMock.Setup(x => x.Users).Returns(users);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(new List<string>());
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _accountService.HandleRefreshTokenAsync(refreshTokenDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal("Authentication Completed Successfulley !", result.message);
            Assert.NotNull(user.RefreshTokens.First(t => t.Token == tokenValue).revokedOn);
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }
    }
}