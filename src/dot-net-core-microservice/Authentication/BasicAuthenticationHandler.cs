using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace dot_net_core_microservice.Authentication;

public static class BasicAuthenticationDefaults
{
    public const string AuthenticationScheme = "Basic";
}

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var headerValue) ||
            !string.Equals(headerValue.Scheme, BasicAuthenticationDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) ||
            headerValue.Parameter is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header."));
        }

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter));
        }
        catch (FormatException)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic auth encoding."));
        }

        var separatorIndex = credentials.IndexOf(':');
        if (separatorIndex < 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic auth format."));
        }

        var username = credentials[..separatorIndex];
        var password = credentials[(separatorIndex + 1)..];

        var expectedUsername = configuration["BasicAuth:Username"];
        var expectedPassword = configuration["BasicAuth:Password"];

        if (!SecretEquals(username, expectedUsername) || !SecretEquals(password, expectedPassword))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"dot-net-core-microservice\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool SecretEquals(string? actual, string? expected)
    {
        if (actual is null || expected is null)
        {
            return false;
        }

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
