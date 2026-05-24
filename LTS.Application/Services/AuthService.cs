using System;
using System.Threading.Tasks;
using LTS.Common.Models;
using LTS.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly LogService _log;

    public AuthService(AppDbContext db, LogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        try
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

            if (user == null)
            {
                await _log.LogErrorAsync($"Login failed — user '{username}' not found");
                return null;
            }

            if (user.PasswordHash != password)
            {
                await _log.LogErrorAsync($"Login failed — wrong password for '{username}'");
                return null;
            }

            SessionManager.CurrentUser = user;

            await _log.LogInfoAsync(
                $"User '{username}' logged in as {user.Role}");

            return user;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Unexpected error during login: {ex.Message}");

            return null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var username = SessionManager.CurrentUser?.UserName;

            SessionManager.CurrentUser = null;

            await _log.LogInfoAsync(
                $"User '{username}' logged out");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Logout error: {ex.Message}");
        }
    }
}