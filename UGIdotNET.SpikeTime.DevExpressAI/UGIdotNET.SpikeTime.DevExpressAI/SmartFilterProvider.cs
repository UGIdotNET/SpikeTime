﻿using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace UGIdotNET.SpikeTime.DevExpressAI;

public class SmartFilterProvider
{
    readonly IEmbeddingGenerator<string, Embedding<float>> Embedder;
    readonly static ConcurrentDictionary<string, Embedding<float>> cache = new(StringComparer.OrdinalIgnoreCase);
    public SmartFilterProvider(IEmbeddingGenerator<string, Embedding<float>> embedder)
    {
        Embedder = embedder;
    }
    public async Task FillCacheAsync(IEnumerable<string> words)
    {
        try
        {
            var nonCachedWords = words.Where(x => !cache.ContainsKey(x)).ToArray();
            if (!nonCachedWords.Any())
                return;
            var embeddings = await Embedder.GenerateAsync(nonCachedWords);
            foreach (var (word, embedding) in nonCachedWords.Zip(embeddings))
            {
                cache[word] = embedding;
            }
        }
        catch
        {
        }
    }
    public float GetSimilarity(string text, string searchText)
    {
        var eFilter = cache[text];
        var eText = cache[searchText];
        var cosineSimilarity = TensorPrimitives.CosineSimilarity(eText.Vector.Span, eFilter.Vector.Span);
        return cosineSimilarity;
    }
}
