using LTS.Application.Data;
using LTS.Application.Services;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LTS.Tests;


public class AuthServiceTests
{
    private AppDbContext CreateInMemoryDb() // create fake db for testing 
    {
        var options = new DbContextOptionsBuilder<AppDbContext>() //This builds configuration for your database
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // fresh db per test
            .Options;

        var db = new AppDbContext(options);

        // seed a test user
        db.Users.Add(new User
        {
            UserId = 1,
            UserName = "Engineer01",
            PasswordHash = "123",
            Role = "Engineer",
            IsActive = true
        });

        db.Users.Add(new User
        {
            UserId = 2,
            UserName = "InactiveUser",
            PasswordHash = "123",
            Role = "Operator",
            IsActive = false  // disabled account
        });

        db.SaveChanges();
        return db;
    }

    private LogService CreateFakeLogService()
    {
        // mock the log repo so logs don't hit real DB
        var mockLogRepo = new Mock<ILogRepository>();
        mockLogRepo
            .Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
            .Returns(Task.CompletedTask);

        return new LogService(mockLogRepo.Object);
    }

    [Fact]

    // ── TEST 1 — valid login returns user 
    public async Task LoginAsync_ValidCredentials_ReturnsUser()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("Engineer01", "123");

        Assert.NotNull(result);
        Assert.Equal("Engineer01", result.UserName);
        Assert.Equal("Engineer", result.Role);

    }

    //wrong passwornd check
    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("Engineer01", "wrongpassword");

        Assert.Null(result);
    }

    [Fact] // ── TEST 3 — unknown user returns null
    public async Task LoginAsync_UnknownUser_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("Ghost", "123");

        Assert.Null(result);
    }

    // ── TEST 4 — inactive user cannot login 
    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("InactiveUser", "123");

        Assert.Null(result);
    }

    // ── TEST 5 — successful login sets SessionManager 
    [Fact]
    public async Task LoginAsync_ValidCredentials_SetsSession()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        await auth.LoginAsync("Engineer01", "123");

        Assert.NotNull(SessionManager.CurrentUser);
        Assert.Equal("Engineer01", SessionManager.CurrentUser!.UserName);
    }

    // ── TEST 6 — logout clears session
    [Fact]
    public async Task Logout_ClearsSession()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        await auth.LoginAsync("Engineer01", "123");
        Assert.NotNull(SessionManager.CurrentUser); // logged in

       await auth.LogoutAsync();
        Assert.Null(SessionManager.CurrentUser);    // cleared
    }
    // ── TEST 7 — empty username returns null ──────────────────────
    [Fact]
    public async Task LoginAsync_EmptyUsername_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("", "123");

        Assert.Null(result);
    }

    // ── TEST 8 — case sensitive username check ────────────────────
    [Fact]
    public async Task LoginAsync_WrongCase_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var auth = new AuthService(db, CreateFakeLogService());

        var result = await auth.LoginAsync("engineer01", "123"); // lowercase

        Assert.Null(result); // depends on your DB collation
    }


}