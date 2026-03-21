using System.Xml.Linq;

namespace TaxPortalApi.Infrastructure.OpenApi;

internal static class ControllerXmlCommentsProvider
{
    public static IReadOnlyDictionary<string, string> LoadControllerSummaries(string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            return new Dictionary<string, string>();
        }

        var document = XDocument.Load(xmlPath);

        return document
            .Descendants("member")
            .Select(member => new
            {
                Name = member.Attribute("name")?.Value,
                Summary = NormalizeSummary(member.Element("summary")?.Value)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name)
                && item.Name!.StartsWith("T:", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(item.Summary))
            .ToDictionary(item => item.Name![2..], item => item.Summary!, StringComparer.Ordinal);
    }

    private static string? NormalizeSummary(string? summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        var normalized = string.Join(" ", summary.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        return normalized.TrimEnd('。', '.', '；', ';');
    }
}