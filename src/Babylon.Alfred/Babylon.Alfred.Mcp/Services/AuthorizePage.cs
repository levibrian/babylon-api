using System.Text.Encodings.Web;

namespace Babylon.Alfred.Mcp.Services;

public static class AuthorizePage
{
    public static string SignIn(string googleClientId, string requestId, string clientName, string redirectHost)
    {
        var html = HtmlEncoder.Default;
        return Layout($$"""
            <div class="card">
              <img src="/babylon_logo.png" alt="Babylon" class="logo">
              <h1>Sign in to continue</h1>
              <p class="subtitle"><strong>{{html.Encode(clientName)}}</strong> wants to access your Babylon portfolio.</p>
              <p class="subtitle">After sign-in you will be sent back to <span class="mono">{{html.Encode(redirectHost)}}</span>.</p>
              <div id="g_id_onload" data-client_id="{{html.Encode(googleClientId)}}" data-callback="onGoogleCredential" data-auto_prompt="false"></div>
              <div class="g_id_signin" data-type="standard" data-text="continue_with" data-theme="filled_black"></div>
              <form id="signin" method="post" action="/authorize/google">
                <input type="hidden" name="request_id" value="{{html.Encode(requestId)}}">
                <input type="hidden" name="credential" id="credential">
              </form>
            </div>
            <script>
              function onGoogleCredential(response) {
                document.getElementById('credential').value = response.credential;
                document.getElementById('signin').submit();
              }
            </script>
            <script src="https://accounts.google.com/gsi/client" async></script>
            """);
    }

    public static string Consent(string clientName, string userEmail, string redirectHost, string requestId)
    {
        var html = HtmlEncoder.Default;
        return Layout($$"""
            <div class="card">
              <img src="/babylon_logo.png" alt="Babylon" class="logo">
              <h1>Authorize {{html.Encode(clientName)}}</h1>
              <p class="subtitle">Signed in as <span class="mono">{{html.Encode(userEmail)}}</span></p>
              <p class="subtitle"><strong>{{html.Encode(clientName)}}</strong> will be able to:</p>
              <ul class="permissions">
                <li>Ver tus posiciones, transacciones y perfil de inversor</li>
                <li>Modificar transacciones, cash balance y allocation targets</li>
                <li>Importar movimientos desde Trade Republic</li>
              </ul>
              <p class="subtitle">You will be sent back to <span class="mono">{{html.Encode(redirectHost)}}</span>.</p>
              <form method="post" action="/authorize/consent" class="actions">
                <input type="hidden" name="request_id" value="{{html.Encode(requestId)}}">
                <button type="submit" name="decision" value="authorize" class="btn-primary">Authorize</button>
                <button type="submit" name="decision" value="deny" class="btn-secondary">Cancel</button>
              </form>
            </div>
            """);
    }

    public static string Error(string message)
        => Layout($"""
            <div class="card">
              <img src="/babylon_logo.png" alt="Babylon" class="logo">
              <p class="subtitle">{HtmlEncoder.Default.Encode(message)}</p>
            </div>
            """);

    private static string Layout(string body) => $"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>Babylon sign-in</title>
          <link rel="stylesheet" href="/styles/authorize.css">
        </head>
        <body>
        {body}
        </body>
        </html>
        """;
}
