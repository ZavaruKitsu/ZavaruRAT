#region

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

#endregion

namespace ZavaruRAT.Main.Authentication;

public sealed class XAuthHandler : AuthenticationHandler<XAuthSchemeOptions>
{
    private readonly IConfiguration _configuration;

    /// <inheritdoc />
    public XAuthHandler(IConfiguration configuration, IOptionsMonitor<XAuthSchemeOptions> options,
                        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,
        clock)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return Task.FromResult(AuthenticateResult.Fail("No token provided (not header)"));
        }

        var header = Request.Headers.Authorization[0];

        if (string.IsNullOrWhiteSpace(header))
        {
            return Task.FromResult(AuthenticateResult.Fail("No token provided (empty)"));
        }

        var split = header.Split(':');

        if (split.Length != 2)
        {
            return Task.FromResult(AuthenticateResult.Fail("Wrong token (length mismatch)"));
        }

        if (split[0] != _configuration["App:Token"])
        {
            return Task.FromResult(AuthenticateResult.Fail("Wrong token (token mismatch)"));
        }

        var claims = new List<Claim>
        {
            new(XClaimTypes.NodeId, split[1])
        };

        var claimsIdentity = new ClaimsIdentity(claims, XAuthSchemeConstants.SchemeName);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        var ticket = new AuthenticationTicket(claimsPrincipal, XAuthSchemeConstants.SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
