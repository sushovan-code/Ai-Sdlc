using System.Text.RegularExpressions;
using AiSdlc.Application.Analysis.Content;

namespace AiSdlc.Application.Analysis.Agents;

/// <summary>
/// The governance agent. Before any text leaves for the model it masks personal data and secrets (and says so),
/// and flags text that looks like an attempt to give the model instructions. After the draft comes back it flags
/// claims the document does not support. It is deterministic, so what it does can be tested and explained.
/// </summary>
public static partial class GovernanceGuard
{
    public const string EmailCode = "pii.email";
    public const string CardCode = "pii.card-number";
    public const string PhoneCode = "pii.phone";
    public const string SecretCode = "secret.credential";
    public const string InjectionCode = "injection.suspected";
    public const string UnsupportedCode = "claim.unsupported";

    private const int MinPhoneDigits = 10;
    private const int MinCardDigits = 13;
    private const int MaxCardDigits = 19;
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    public static RedactedText Redact(string text)
    {
        var flags = new List<GuardrailFlag>();
        var result = text;

        result = Mask(result, SecretPattern(), _ => true, "[redacted-secret]", SecretCode, "credential or secret value(s)", flags);
        result = Mask(result, EmailPattern(), _ => true, "[redacted-email]", EmailCode, "email address(es)", flags);
        result = Mask(result, DigitRunPattern(), IsCardNumber, "[redacted-card-number]", CardCode, "possible payment card number(s)", flags);
        result = Mask(result, DigitRunPattern(), IsPhoneNumber, "[redacted-phone]", PhoneCode, "phone number(s)", flags);

        if (InjectionPattern().IsMatch(text))
        {
            flags.Add(new GuardrailFlag(
                InjectionCode,
                "The document contains wording that looks like instructions to an AI. It was treated as data and not followed."));
        }

        return new RedactedText(result, flags);
    }

    /// <summary>Flags items that no quote in the document backs up.</summary>
    public static IReadOnlyList<GuardrailFlag> CheckSupport(IReadOnlyList<string> unsupportedItemIds) =>
        unsupportedItemIds.Count == 0
            ? []
            : [new GuardrailFlag(
                UnsupportedCode,
                $"No quote in the document supports: {string.Join(", ", unsupportedItemIds)}. Check these before approving.")];

    private static string Mask(
        string text,
        Regex pattern,
        Func<string, bool> qualifies,
        string replacement,
        string code,
        string description,
        List<GuardrailFlag> flags)
    {
        var count = 0;
        var masked = pattern.Replace(text, match =>
        {
            if (!qualifies(match.Value))
            {
                return match.Value;
            }

            count++;
            return replacement;
        });

        if (count > 0)
        {
            flags.Add(new GuardrailFlag(code, $"{count} {description} were masked before the document was analysed."));
        }

        return masked;
    }

    private static bool IsCardNumber(string text)
    {
        var digits = text.Where(char.IsDigit).ToArray();
        return digits.Length is >= MinCardDigits and <= MaxCardDigits && PassesLuhn(digits);
    }

    private static bool IsPhoneNumber(string text) =>
        text.Count(char.IsDigit) >= MinPhoneDigits && !IsCardNumber(text) && text.Any(c => c is ' ' or '-' or '(' or ')' or '+' or '.');

    private static bool PassesLuhn(char[] digits)
    {
        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var value = digits[i] - '0';
            if (doubleIt)
            {
                value *= 2;
                value = value > 9 ? value - 9 : value;
            }

            sum += value;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\+?\d[\d ().\-]{8,}\d", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex DigitRunPattern();

    [GeneratedRegex(
        @"-----BEGIN [A-Z ]*PRIVATE KEY-----|\b(?:sk-[A-Za-z0-9_\-]{20,}|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{30,})\b|\b(?:password|passwd|secret|api[_\-]?key|token)\s*[:=]\s*\S+",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 2000)]
    private static partial Regex SecretPattern();

    [GeneratedRegex(
        @"ignore\s+(?:all\s+|any\s+|the\s+)?(?:previous|prior|above)\s+(?:instructions|prompts?)|disregard\s+(?:all\s+|the\s+)?(?:previous\s+)?instructions|reveal\s+(?:your|the)\s+(?:system\s+)?(?:prompt|instructions)|you\s+are\s+now\s+(?:an?\s+)?(?:unrestricted|jailbroken)",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 2000)]
    private static partial Regex InjectionPattern();
}
