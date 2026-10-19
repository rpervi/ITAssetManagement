using System.Security.Claims;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ITAssetManagement.Api.Controllers;

[ApiController]
[EnableRateLimiting("authentication")]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IAntiforgery antiforgery,
    IOptionsMonitor<CookieAuthenticationOptions> cookieOptions) : ControllerBase
{
    private const string RefreshCookieName = "__Host-ITAM.Refresh";
    private const string XsrfRequestCookieName = "__Host-ITAM.Xsrf-Request";
    private readonly CookieAuthenticationOptions _cookieOptions =
        cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme);

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(
            XsrfRequestCookieName,
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true,
            });
        return Ok(new { requestToken = tokens.RequestToken });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthenticatedUserDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await authService.LoginAsync(request, cancellationToken);
        if (session is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        await SignInAsync(session);
        SetRefreshCookie(session.RefreshToken, session.ExpiresOn);
        return Ok(session.User);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Guid.TryParse(User.FindFirstValue(AuthClaims.SessionId), out var sessionId))
        {
            await authService.LogoutAsync(sessionId, cancellationToken);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(RefreshCookieName, SecureCookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("current-user")]
    public async Task<ActionResult<AuthenticatedUserDto>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthenticatedUserDto>> RefreshSession(
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var refreshToken))
        {
            return Unauthorized();
        }

        var session = await authService.RefreshSessionAsync(refreshToken, cancellationToken);
        if (session is null)
        {
            Response.Cookies.Delete(RefreshCookieName, SecureCookieOptions());
            return Unauthorized();
        }

        await SignInAsync(session);
        SetRefreshCookie(session.RefreshToken, session.ExpiresOn);
        return Ok(session.User);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return Accepted(new
        {
            message = "If an active account matches that email, password reset instructions will be sent.",
        });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var reset = await authService.ResetPasswordAsync(request, cancellationToken);
        return reset
            ? NoContent()
            : BadRequest(new { message = "The reset link is invalid or expired, or the password does not meet requirements." });
    }

    private async Task SignInAsync(AuthSessionDto session)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.User.UserId.ToString()),
            new(ClaimTypes.Name, session.User.Username),
            new(ClaimTypes.Email, session.User.Email),
            new(AuthClaims.SessionId, session.SessionId.ToString()),
        };
        claims.AddRange(session.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var expires = DateTimeOffset.UtcNow.AddMinutes(_cookieOptions.ExpireTimeSpan.TotalMinutes);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                ExpiresUtc = expires,
            });
    }

    private void SetRefreshCookie(string token, DateTime expiresOn)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresOn, DateTimeKind.Utc)),
                IsEssential = true,
            });
    }

    private static CookieOptions SecureCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true,
        };
    }
}

public static class AuthClaims
{
    public const string SessionId = "itam_session_id";
}
