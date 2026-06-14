using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CabinConnect.Api.Tests.Auth;

/// <summary>
/// Authenticates requests that carry the X-Test-Auth header; challenges without it return 401.
/// Used in WebApplicationFactory tests in place of the real Supabase JWT scheme.
/// </summary>
public sealed class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme  = "FakeAuth";
    public const string GuestId    = "aaaaaaaa-0000-0000-0000-000000000001";
    public const string AuthHeader = "X-Test-Auth";

    public FakeAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(AuthHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims    = new[] { new Claim("sub", GuestId) };
        var identity  = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    // Return 401 rather than redirecting or throwing when challenge runs unauthenticated.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}
