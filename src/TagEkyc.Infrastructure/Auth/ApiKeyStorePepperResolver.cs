using System.Text;
using TagEkyc.Infrastructure.Secrets;

namespace TagEkyc.Infrastructure.Auth;

public static class ApiKeyStorePepperResolver
{
    public const string Unavailable = "PROD_APIKEY_PEPPER_SECRET_UNAVAILABLE";
    public const string RefInvalid = "PROD_APIKEY_PEPPER_SECRET_REF_INVALID";

    public static ApiKeyStorePepper Resolve(string? secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            throw new InvalidOperationException(Unavailable);
        }

        try
        {
            var value = SecretRefResolver.Resolve(secretRef).Value;
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length < 32)
            {
                throw new InvalidOperationException(RefInvalid);
            }

            return new ApiKeyStorePepper(bytes);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (SecretRefResolutionException exception) when (exception.ErrorKind == SecretRefErrorKind.Invalid)
        {
            throw new InvalidOperationException(RefInvalid);
        }
        catch (SecretRefResolutionException exception) when (exception.ErrorKind == SecretRefErrorKind.Missing)
        {
            throw new InvalidOperationException(Unavailable);
        }
    }
}
