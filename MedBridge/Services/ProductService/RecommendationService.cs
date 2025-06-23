using MathNet.Numerics.LinearAlgebra;
using MedBridge.Models;
using MedBridge.Models.ProductModels;
using MoviesApi.models;
using System.Text.RegularExpressions;

namespace MedBridge.Services;

public class RecommendationService
{
    private readonly ApplicationDbContext _context;
    private Matrix<double> _cosineSimilarityMatrix;
    private List<ProductModel> _products;

    public RecommendationService(ApplicationDbContext context)
    {
        _context = context;
        InitializeModel();
    }

    private void InitializeModel()
    {
        _products = _context.Products.ToList();
        if (!_products.Any()) return;

        var cleanedDescriptions = _products.Select(p => CleanText(p.Description)).ToList();
        var tfidfMatrix = ComputeTfIdf(cleanedDescriptions);
        _cosineSimilarityMatrix = ComputeCosineSimilarity(tfidfMatrix);
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = text.ToLower().Trim();
        text = Regex.Replace(text, "[^a-z0-9\\s]", "");
        return text;
    }

    private Matrix<double> ComputeTfIdf(List<string> documents)
    {
        var vocabulary = documents.SelectMany(d => d.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();
        var termCount = vocabulary.Count;
        var docCount = documents.Count;

        var tfidf = Matrix<double>.Build.Dense(docCount, termCount);
        for (int i = 0; i < docCount; i++)
        {
            var words = documents[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var wordCounts = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
            foreach (var word in wordCounts.Keys)
            {
                int termIndex = vocabulary.IndexOf(word);
                if (termIndex >= 0)
                {
                    double tf = (double)wordCounts[word] / words.Length;
                    double idf = Math.Log((double)docCount / (1 + documents.Count(d => d.Contains(word))));
                    tfidf[i, termIndex] = tf * idf;
                }
            }
        }
        return tfidf;
    }

    private Matrix<double> ComputeCosineSimilarity(Matrix<double> tfidfMatrix)
    {
        var rowCount = tfidfMatrix.RowCount;
        var similarity = Matrix<double>.Build.Dense(rowCount, rowCount);
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < rowCount; j++)
            {
                var vec1 = tfidfMatrix.Row(i);
                var vec2 = tfidfMatrix.Row(j);
                double dotProduct = vec1.DotProduct(vec2);
                double norm1 = Math.Sqrt(vec1.DotProduct(vec1));
                double norm2 = Math.Sqrt(vec2.DotProduct(vec2));
                similarity[i, j] = norm1 * norm2 > 0 ? dotProduct / (norm1 * norm2) : 0;
            }
        }
        return similarity;
    }

    public List<ProductModel> GetSimilarProducts(int productId, int topN = 3)
    {
        var idx = _products.FindIndex(p => p.ProductId == productId);
        if (idx < 0) throw new ArgumentException($"Product ID {productId} not found");

        var similarities = _cosineSimilarityMatrix.Row(idx).ToArray();
        var similarIndices = similarities.Select((sim, i) => (sim, i))
            .OrderByDescending(x => x.sim)
            .Skip(1)
            .Take(topN)
            .Select(x => x.i)
            .ToList();

        return similarIndices.Select(i => _products[i]).ToList();
    }
}