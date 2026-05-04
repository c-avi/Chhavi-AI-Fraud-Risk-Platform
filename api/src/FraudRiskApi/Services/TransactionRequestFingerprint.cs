using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FraudRiskApi.Models;

namespace FraudRiskApi.Services;

public static class TransactionRequestFingerprint
{
    public static string Compute(TransactionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Deterministic canonical form for idempotent replay vs. conflict detection
        var builder = new StringBuilder(128);
        builder.Append(request.UserId.Trim());
        builder.Append('|');
        builder.Append(request.Amount.ToString("F2", CultureInfo.InvariantCulture));
        builder.Append('|');
        builder.Append(request.Location.Trim());
        builder.Append('|');
        builder.Append(request.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
