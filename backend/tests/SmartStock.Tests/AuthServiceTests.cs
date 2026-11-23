using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using SmartStock.Core.DTOs;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Services;
using Xunit;

namespace SmartStock.Tests;

public class AuthServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public AuthServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Jwt:Secret", "SmartStockSuperSecureJwtSigningKeyForTestingPurposeOnly12345!"},
            {"Jwt:Issuer", "SmartStockAPI"},
            {"Jwt:Audience", "SmartStockClient"},
            {"Jwt:AccessTokenExpirationMinutes", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _currentUserServiceMock = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesBusinessAndOwnerSuccessfully()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new AuthService(context, _configuration, _currentUserServiceMock.Object);

        var dto = new RegisterRequestDto
        {
            FullName = "Aman Gupta",
            Email = "aman@kirana.in",
            Password = "Password123!",
            BusinessName = "Gupta Provision Store",
            Phone = "+91 98765 00000",
            Address = "Chandni Chowk, Delhi",
            BusinessType = "Kirana"
        };

        // Act
        var result = await service.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Data.User.Email.Should().Be("aman@kirana.in");
        result.Data.User.Role.Should().Be(UserRole.Owner);
        result.Data.Business.BusinessName.Should().Be("Gupta Provision Store");

        // Verify in database
        var userInDb = context.Users.FirstOrDefault(u => u.Email == "aman@kirana.in");
        userInDb.Should().NotBeNull();
        userInDb!.FullName.Should().Be("Aman Gupta");
        userInDb.Role.Should().Be(UserRole.Owner);

        var businessInDb = context.Businesses.FirstOrDefault(b => b.Id == userInDb.BusinessId);
        businessInDb.Should().NotBeNull();
        businessInDb!.BusinessName.Should().Be("Gupta Provision Store");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new AuthService(context, _configuration, _currentUserServiceMock.Object);

        var dto = new RegisterRequestDto
        {
            FullName = "Ramesh Kumar",
            Email = "ramesh@store.in",
            Password = "Password123!",
            BusinessName = "Ramesh Kirana"
        };

        await service.RegisterAsync(dto);

        // Act & Assert
        var act = async () => await service.RegisterAsync(dto);
        await act.Should().ThrowAsync<ConflictAppException>();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new AuthService(context, _configuration, _currentUserServiceMock.Object);

        var regDto = new RegisterRequestDto
        {
            FullName = "Sunil Verma",
            Email = "sunil@delhistore.com",
            Password = "SecretPassword123!",
            BusinessName = "Verma Traders"
        };
        await service.RegisterAsync(regDto);

        var loginDto = new LoginRequestDto
        {
            Email = "sunil@delhistore.com",
            Password = "SecretPassword123!"
        };

        // Act
        var loginResult = await service.LoginAsync(loginDto);

        // Assert
        loginResult.Should().NotBeNull();
        loginResult.Success.Should().BeTrue();
        loginResult.Data!.Token.Should().NotBeNullOrWhiteSpace();
        loginResult.Data.User.Email.Should().Be("sunil@delhistore.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        using var context = TestDbContextFactory.CreateInMemoryDbContext();
        var service = new AuthService(context, _configuration, _currentUserServiceMock.Object);

        var regDto = new RegisterRequestDto
        {
            FullName = "Sunil Verma",
            Email = "sunil2@delhistore.com",
            Password = "CorrectPassword123!",
            BusinessName = "Verma Traders 2"
        };
        await service.RegisterAsync(regDto);

        var loginDto = new LoginRequestDto
        {
            Email = "sunil2@delhistore.com",
            Password = "WrongPassword999!"
        };

        // Act & Assert
        var act = async () => await service.LoginAsync(loginDto);
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }
}
