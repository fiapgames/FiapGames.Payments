using System.Security.Cryptography;
using System.Text;
using Amazon.Runtime;

namespace FiapGames.Payments.Services;

/// <summary>
/// Assina as chamadas à Function URL da Lambda de notificações (AuthType AWS_IAM)
/// com SigV4. As credenciais vêm da cadeia padrão da AWS: no EKS, o EKS Pod
/// Identity as injeta no pod; localmente, variáveis AWS_* ou ~/.aws. Sem
/// credenciais a requisição segue sem assinatura (útil contra o Notifications
/// rodando via dotnet run, que não tem autenticação).
/// </summary>
public sealed class SigV4Handler(IConfiguration configuration, ILogger<SigV4Handler> logger)
    : DelegatingHandler
{
    private const string Service = "lambda";

    private readonly string _region = configuration["Notifications:Region"] ?? "us-east-2";
    private AWSCredentials? _credentials;
    private bool _warnedMissingCredentials;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var credentials = await ResolveCredentialsAsync();
        if (credentials is not null)
        {
            await SignAsync(request, credentials, cancellationToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<ImmutableCredentials?> ResolveCredentialsAsync()
    {
        try
        {
            _credentials ??= FallbackCredentialsFactory.GetCredentials();
            return await _credentials.GetCredentialsAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (!_warnedMissingCredentials)
            {
                _warnedMissingCredentials = true;
                logger.LogWarning(exception,
                    "Credenciais AWS não encontradas: notificações seguirão sem assinatura SigV4");
            }

            _credentials = null;
            return null;
        }
    }

    private async Task SignAsync(
        HttpRequestMessage request, ImmutableCredentials credentials, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'");
        var dateStamp = now.ToString("yyyyMMdd");

        var body = Array.Empty<byte>();
        if (request.Content is not null)
        {
            await request.Content.LoadIntoBufferAsync(cancellationToken);
            body = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        var payloadHash = Hex(SHA256.HashData(body));
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = uri.Authority,
            ["x-amz-content-sha256"] = payloadHash,
            ["x-amz-date"] = amzDate
        };
        if (!string.IsNullOrEmpty(credentials.Token))
        {
            headers["x-amz-security-token"] = credentials.Token;
        }

        var signedHeaders = string.Join(';', headers.Keys);
        var canonicalHeaders = string.Concat(headers.Select(header => $"{header.Key}:{header.Value}\n"));
        var canonicalRequest = string.Join('\n',
            request.Method.Method, uri.AbsolutePath, CanonicalQuery(uri), canonicalHeaders, signedHeaders, payloadHash);

        var scope = $"{dateStamp}/{_region}/{Service}/aws4_request";
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256", amzDate, scope, Hex(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest))));

        var signingKey = Hmac(Hmac(Hmac(Hmac(
            Encoding.UTF8.GetBytes("AWS4" + credentials.SecretKey), dateStamp), _region), Service), "aws4_request");
        var signature = Hex(Hmac(signingKey, stringToSign));

        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        if (!string.IsNullOrEmpty(credentials.Token))
        {
            request.Headers.TryAddWithoutValidation("x-amz-security-token", credentials.Token);
        }

        request.Headers.TryAddWithoutValidation("Authorization",
            $"AWS4-HMAC-SHA256 Credential={credentials.AccessKey}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    private static string CanonicalQuery(Uri uri) =>
        string.Join('&', uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Order(StringComparer.Ordinal));

    private static byte[] Hmac(byte[] key, string data) => HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(data));

    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
}
