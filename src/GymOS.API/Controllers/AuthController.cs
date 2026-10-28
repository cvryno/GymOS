using BCrypt.Net;
using Google.Apis.Auth;
using GymOS.Application.DTOs;
using GymOS.Application.Interfaces;
using GymOS.Domain.Entities;
using GymOS.Domain.Enums;
using GymOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly GymDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AuthController(GymDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
        if (user is null || user.PasswordHash is null)
            return Unauthorized(new { message = "Credenciales inválidas" });

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciales inválidas" });

        return await GenerateAuthResponse(user);
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin([FromBody] GoogleLoginDto dto)
    {
        try
        {
            var clientId = _config["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("YOUR_", StringComparison.Ordinal))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "El inicio de sesión con Google no está configurado" });

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);

            if (!payload.EmailVerified)
                return Unauthorized(new { message = "La cuenta de Google debe tener el correo verificado" });

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user is null)
            {
                user = new AppUser
                {
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "",
                    LastName = payload.FamilyName ?? "",
                    GoogleId = payload.Subject,
                    PhotoUrl = payload.Picture,
                    Role = UserRole.Receptionist,
                    IsActive = true
                };
                _db.AppUsers.Add(user);
                await _db.SaveChangesAsync();
            }
            else
            {
                if (!user.IsActive)
                    return Unauthorized(new { message = "La cuenta está desactivada" });

                if (!string.IsNullOrEmpty(user.GoogleId) && user.GoogleId != payload.Subject)
                    return Unauthorized(new { message = "La cuenta de Google no coincide" });

                user.GoogleId = payload.Subject;
                user.PhotoUrl = payload.Picture;
                await _db.SaveChangesAsync();
            }

            return await GenerateAuthResponse(user);
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Token de Google inválido" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] string refreshToken)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken &&
            u.RefreshTokenExpiry > DateTime.UtcNow &&
            u.IsActive);

        if (user is null)
            return Unauthorized(new { message = "Token de actualización inválido" });

        return await GenerateAuthResponse(user);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (await _db.AppUsers.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "El email ya está registrado" });

        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || role != UserRole.Receptionist)
            return BadRequest(new { message = "Solo se permite registrar usuarios con rol de recepción" });

        var user = new AppUser
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            IsActive = true
        };

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();
        return await GenerateAuthResponse(user);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _db.AppUsers.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id, user.Email, user.FirstName, user.LastName,
            user.FullName, Role = user.Role.ToString(), user.PhotoUrl
        });
    }

    private async Task<AuthResponseDto> GenerateAuthResponse(AppUser user)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new AuthResponseDto(accessToken, refreshToken, user.Email,
            user.FullName, user.Role.ToString(), user.PhotoUrl);
    }
}
