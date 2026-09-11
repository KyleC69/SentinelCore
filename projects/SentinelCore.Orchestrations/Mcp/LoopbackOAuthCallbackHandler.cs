// Solution: SentinelCore
// Project:   SentinelCore.Orchestrations
// File:         LoopbackOAuthCallbackHandler.cs
// Author: Kyle L. Crowder
// Build Num:  091112



using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;

using ModelContextProtocol.Authentication;




namespace SentinelCore.Orchestrations.Mcp;





/// <summary>
///     Handles the OAuth authorization-code callback by opening the system browser
///     and listening on a temporary loopback address for the authorization response.
/// </summary>
public static class LoopbackOAuthCallbackHandler
{
    private const string DefaultLoopbackPrefix = "http://127.0.0.1:0/";








    /// <summary>
    ///     Builds a <see cref="ClientOAuthOptions.AuthorizationCallbackHandler" /> delegate that
    ///     launches the system browser and captures the authorization response on a loopback redirect URI.
    /// </summary>
    /// <param name="preferredCallbackUri">Optional preferred loopback URI. A free port is used when port is 0.</param>
    /// <returns>A callback handler compatible with <see cref="ClientOAuthOptions.AuthorizationCallbackHandler" />.</returns>
    public static Func<AuthorizationCallbackContext, CancellationToken, Task<AuthorizationResult?>> Create(Uri? preferredCallbackUri = null)
    {
        return async (context, cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(context);

            Uri callbackUri = preferredCallbackUri ?? context.RedirectUri ?? new Uri(DefaultLoopbackPrefix);
            using HttpListener listener = new();
            listener.Prefixes.Add(GetListenerPrefix(callbackUri));
            listener.Start();

            try
            {
                OpenBrowser(context.AuthorizationUri);

                HttpListenerContext httpContext = await listener.GetContextAsync().ConfigureAwait(false);

                NameValueCollection query = HttpUtility.ParseQueryString(httpContext.Request.Url?.Query ?? string.Empty);
                string? code = query.Get("code");
                string? state = query.Get("state");
                string? iss = query.Get("iss");
                string? error = query.Get("error");

                await SendResponseAsync(httpContext.Response, string.IsNullOrEmpty(error)).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException($"OAuth authorization error: {error}");
                }

                if (string.IsNullOrEmpty(code))
                {
                    throw new InvalidOperationException("OAuth callback did not contain an authorization code.");
                }

                return new AuthorizationResult { Code = code, State = state, Iss = iss };
            }
            finally
            {
                listener.Stop();
            }
        };
    }








    private static string GetListenerPrefix(Uri callbackUri)
    {
        // HttpListener requires a prefix ending in '/' and uses http://host:port/.
        string host = callbackUri.Host;
        int port = callbackUri.Port;

        // Port 0 lets HttpListener pick a free port.
        return $"http://{host}:{port}/";
    }








    private static void OpenBrowser(Uri url)
    {
        ProcessStartInfo psi = new(url.AbsoluteUri) { UseShellExecute = true };
        Process.Start(psi);
    }








    private static async Task SendResponseAsync(HttpListenerResponse response, bool success)
    {
        string body = success ? "<html><body><p>Authorization succeeded. You may close this window.</p></body></html>" : "<html><body><p>Authorization failed. Please return to SentinelCore.</p></body></html>";

        byte[] buffer = Encoding.UTF8.GetBytes(body);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html; charset=utf-8";

        await response.OutputStream.WriteAsync(buffer, CancellationToken.None).ConfigureAwait(false);
        response.Close();
    }
}