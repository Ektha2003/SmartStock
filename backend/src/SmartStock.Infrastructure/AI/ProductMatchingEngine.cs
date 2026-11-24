using System.Text.RegularExpressions;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;

namespace SmartStock.Infrastructure.AI;

public class ProductMatchEvaluation
{
    public Product? MatchedProduct { get; set; }
    public ProductMatchStatus Status { get; set; } = ProductMatchStatus.NoMatch;
    public decimal Confidence { get; set; } = 0.0m;
    public string Explanation { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1.0m;
    public decimal? PackSize { get; set; }
    public UnitType TargetUnit { get; set; } = UnitType.Piece;
    public decimal CalculatedStockQuantity { get; set; }
}

public static class ProductMatchingEngine
{
    public static ProductMatchEvaluation EvaluateMatch(
        string rawProductName,
        string? rawDescription,
        string? rawSupplierSku,
        string? rawHsnCode,
        decimal invoiceQuantity,
        string invoiceUnit,
        decimal invoiceUnitPrice,
        IReadOnlyList<Product> catalogProducts)
    {
        if (string.IsNullOrWhiteSpace(rawProductName) || catalogProducts.Count == 0)
        {
            return new ProductMatchEvaluation
            {
                Status = ProductMatchStatus.NoMatch,
                Confidence = 0.0m,
                Explanation = "No product name or inventory is empty.",
                ConversionFactor = 1.0m,
                CalculatedStockQuantity = invoiceQuantity
            };
        }

        var normalizedRawName = NormalizeText(rawProductName);
        var rawTokens = Tokenize(normalizedRawName);

        // 1. Exact SKU match
        if (!string.IsNullOrWhiteSpace(rawSupplierSku))
        {
            var skuMatch = catalogProducts.FirstOrDefault(p =>
                string.Equals(p.SKU.Trim(), rawSupplierSku.Trim(), StringComparison.OrdinalIgnoreCase));

            if (skuMatch != null)
            {
                var conversion = DetectPackSizeAndConversion(rawProductName, invoiceQuantity, invoiceUnit, skuMatch.Unit);
                return new ProductMatchEvaluation
                {
                    MatchedProduct = skuMatch,
                    Status = ProductMatchStatus.ExactMatch,
                    Confidence = 1.0m,
                    Explanation = $"Exact SKU match with '{skuMatch.SKU}'.",
                    ConversionFactor = conversion.ConversionFactor,
                    PackSize = conversion.PackSize,
                    TargetUnit = skuMatch.Unit,
                    CalculatedStockQuantity = invoiceQuantity * conversion.ConversionFactor
                };
            }
        }

        // 2. Exact Name match
        var exactNameMatch = catalogProducts.FirstOrDefault(p =>
            string.Equals(NormalizeText(p.Name), normalizedRawName, StringComparison.OrdinalIgnoreCase));

        if (exactNameMatch != null)
        {
            var conversion = DetectPackSizeAndConversion(rawProductName, invoiceQuantity, invoiceUnit, exactNameMatch.Unit);
            return new ProductMatchEvaluation
            {
                MatchedProduct = exactNameMatch,
                Status = ProductMatchStatus.ExactMatch,
                Confidence = 1.0m,
                Explanation = $"Exact product name match with '{exactNameMatch.Name}'.",
                ConversionFactor = conversion.ConversionFactor,
                PackSize = conversion.PackSize,
                TargetUnit = exactNameMatch.Unit,
                CalculatedStockQuantity = invoiceQuantity * conversion.ConversionFactor
            };
        }

        // 3. Fuzzy token similarity scoring
        var candidateScores = new List<(Product Product, decimal Score, string Reason)>();

        foreach (var p in catalogProducts)
        {
            var pNormalized = NormalizeText(p.Name);
            var pTokens = Tokenize(pNormalized);

            var jaccard = CalculateJaccard(rawTokens, pTokens);
            var lev = CalculateLevenshteinSimilarity(normalizedRawName, pNormalized);

            // Blended score
            var score = (jaccard * 0.65m) + (lev * 0.35m);

            // Substring or token subset bonus
            var allCatalogTokensPresent = pTokens.Count > 0 && pTokens.All(t => rawTokens.Contains(t));
            if (allCatalogTokensPresent)
            {
                score = Math.Max(score, 0.88m);
            }
            else if (normalizedRawName.Contains(pNormalized, StringComparison.OrdinalIgnoreCase) ||
                pNormalized.Contains(normalizedRawName, StringComparison.OrdinalIgnoreCase))
            {
                score = Math.Min(1.0m, score + 0.15m);
            }

            if (score >= 0.40m)
            {
                candidateScores.Add((p, score, $"Token overlap {Math.Round(score * 100)}%"));
            }
        }

        candidateScores = candidateScores.OrderByDescending(c => c.Score).ToList();

        if (candidateScores.Count == 0)
        {
            return new ProductMatchEvaluation
            {
                Status = ProductMatchStatus.NoMatch,
                Confidence = 0.0m,
                Explanation = "No matching product found in inventory catalog.",
                ConversionFactor = 1.0m,
                CalculatedStockQuantity = invoiceQuantity
            };
        }

        var top = candidateScores[0];

        // Check if multiple close matches
        if (candidateScores.Count > 1 && (top.Score - candidateScores[1].Score) < 0.05m && top.Score >= 0.70m)
        {
            var conv = DetectPackSizeAndConversion(rawProductName, invoiceQuantity, invoiceUnit, top.Product.Unit);
            return new ProductMatchEvaluation
            {
                MatchedProduct = top.Product,
                Status = ProductMatchStatus.MultipleMatches,
                Confidence = Math.Round(top.Score, 2),
                Explanation = $"Multiple close matches found ('{top.Product.Name}', '{candidateScores[1].Product.Name}'). Please confirm manually.",
                ConversionFactor = conv.ConversionFactor,
                PackSize = conv.PackSize,
                TargetUnit = top.Product.Unit,
                CalculatedStockQuantity = invoiceQuantity * conv.ConversionFactor
            };
        }

        var topConv = DetectPackSizeAndConversion(rawProductName, invoiceQuantity, invoiceUnit, top.Product.Unit);

        if (top.Score >= 0.85m)
        {
            return new ProductMatchEvaluation
            {
                MatchedProduct = top.Product,
                Status = ProductMatchStatus.HighConfidenceMatch,
                Confidence = Math.Round(top.Score, 2),
                Explanation = $"High confidence match with '{top.Product.Name}' ({Math.Round(top.Score * 100)}% similarity).",
                ConversionFactor = topConv.ConversionFactor,
                PackSize = topConv.PackSize,
                TargetUnit = top.Product.Unit,
                CalculatedStockQuantity = invoiceQuantity * topConv.ConversionFactor
            };
        }

        if (top.Score >= 0.50m)
        {
            return new ProductMatchEvaluation
            {
                MatchedProduct = top.Product,
                Status = ProductMatchStatus.PossibleMatch,
                Confidence = Math.Round(top.Score, 2),
                Explanation = $"Possible match with '{top.Product.Name}' ({Math.Round(top.Score * 100)}% similarity). Review required.",
                ConversionFactor = topConv.ConversionFactor,
                PackSize = topConv.PackSize,
                TargetUnit = top.Product.Unit,
                CalculatedStockQuantity = invoiceQuantity * topConv.ConversionFactor
            };
        }

        return new ProductMatchEvaluation
        {
            Status = ProductMatchStatus.NoMatch,
            Confidence = Math.Round(top.Score, 2),
            Explanation = $"Low confidence match ({Math.Round(top.Score * 100)}%). New product recommended.",
            ConversionFactor = 1.0m,
            CalculatedStockQuantity = invoiceQuantity
        };
    }

    public static (decimal ConversionFactor, decimal? PackSize) DetectPackSizeAndConversion(
        string rawProductName,
        decimal invoiceQuantity,
        string invoiceUnit,
        UnitType targetUnit)
    {
        // Try regex match for pack sizes: 5kg, 5 kg, 10kg, 500g, 1L, 2 Litre, 12 pcs, 24 pcs
        var lower = rawProductName.ToLowerInvariant();

        // Check for KG / Grams
        var kgMatch = Regex.Match(lower, @"(\d+(\.\d+)?)\s*(kg|kilo|kilogram)");
        if (kgMatch.Success && decimal.TryParse(kgMatch.Groups[1].Value, out var kgVal))
        {
            if (targetUnit == UnitType.Kg)
            {
                return (kgVal, kgVal);
            }
            if (targetUnit == UnitType.Gram)
            {
                return (kgVal * 1000m, kgVal);
            }
            return (kgVal, kgVal);
        }

        var gramMatch = Regex.Match(lower, @"(\d+(\.\d+)?)\s*(g|gm|gram|grams)\b");
        if (gramMatch.Success && decimal.TryParse(gramMatch.Groups[1].Value, out var gVal))
        {
            if (targetUnit == UnitType.Kg)
            {
                return (gVal / 1000m, gVal);
            }
            if (targetUnit == UnitType.Gram)
            {
                return (gVal, gVal);
            }
        }

        // Check for Litre / ML
        var ltrMatch = Regex.Match(lower, @"(\d+(\.\d+)?)\s*(l|ltr|litre|liter)");
        if (ltrMatch.Success && decimal.TryParse(ltrMatch.Groups[1].Value, out var ltrVal))
        {
            return (ltrVal, ltrVal);
        }

        var mlMatch = Regex.Match(lower, @"(\d+(\.\d+)?)\s*(ml|millilitre)");
        if (mlMatch.Success && decimal.TryParse(mlMatch.Groups[1].Value, out var mlVal))
        {
            if (targetUnit == UnitType.Litre)
            {
                return (mlVal / 1000m, mlVal);
            }
            return (mlVal, mlVal);
        }

        // Check for Box/Pack of N pieces
        var packMatch = Regex.Match(lower, @"(\d+)\s*(pcs|pieces|units|count|pack|box)\b");
        if (packMatch.Success && decimal.TryParse(packMatch.Groups[1].Value, out var packVal) && packVal > 1)
        {
            if (targetUnit == UnitType.Piece)
            {
                return (packVal, packVal);
            }
        }

        return (1.0m, null);
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var cleaned = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\s]", " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static HashSet<string> Tokenize(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pack", "packet", "pouch", "box", "bottle", "premium", "pure", "best", "of", "and", "the", "with", "in"
        };

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !stopWords.Contains(t));

        return new HashSet<string>(tokens, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal CalculateJaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0m;
        if (a.Count == 0 || b.Count == 0) return 0.0m;

        var intersection = a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count();
        var union = a.Union(b, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (decimal)intersection / union : 0.0m;
    }

    private static decimal CalculateLevenshteinSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0m;
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0m;

        int len1 = s1.Length;
        int len2 = s2.Length;

        int[,] d = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++) d[i, 0] = i;
        for (int j = 0; j <= len2; j++) d[0, j] = j;

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        int maxLen = Math.Max(len1, len2);
        return 1.0m - ((decimal)d[len1, len2] / maxLen);
    }
}
