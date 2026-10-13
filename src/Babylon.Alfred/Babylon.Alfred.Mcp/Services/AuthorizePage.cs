using System.Text.Encodings.Web;

namespace Babylon.Alfred.Mcp.Services;

public static class AuthorizePage
{
    public static string SignIn(string googleClientId, string requestId, string clientName, string redirectHost)
    {
        var html = HtmlEncoder.Default;
        return Layout($$"""
            <p><strong>{{html.Encode(clientName)}}</strong> wants to access your Babylon portfolio.</p>
            <p>After sign-in you will be sent back to <strong>{{html.Encode(redirectHost)}}</strong>.</p>
            <div id="g_id_onload" data-client_id="{{html.Encode(googleClientId)}}" data-callback="onGoogleCredential" data-auto_prompt="false"></div>
            <div class="g_id_signin" data-type="standard" data-text="continue_with"></div>
            <form id="signin" method="post" action="/authorize/google">
              <input type="hidden" name="request_id" value="{{html.Encode(requestId)}}">
              <input type="hidden" name="credential" id="credential">
            </form>
            <script>
              function onGoogleCredential(response) {
                document.getElementById('credential').value = response.credential;
                document.getElementById('signin').submit();
              }
            </script>
            <script src="https://accounts.google.com/gsi/client" async></script>
            """);
    }

    public static string Error(string message)
        => Layout($"<p>{HtmlEncoder.Default.Encode(message)}</p>");

    private static string Layout(string body) => $"""
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Babylon sign-in</title></head>
        <body style="font-family: system-ui, sans-serif; max-width: 32rem; margin: 4rem auto; padding: 0 1rem;">
        <h1>Babylon</h1>
        {body}
        </body>
        </html>
        """;
}
