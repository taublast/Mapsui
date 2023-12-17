using Mapsui.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mapsui.Layers;

namespace Mapsui.Extensions;

public static class FeatureExtensions
{
    public static Func<IFeature, IFeature> DefaultCopy = StandardCopy;
    private static readonly Dictionary<Type, Func<IFeature, IFeature>> _copyFeature = new();

    public static void RegisterFeature<T>(Func<IFeature, IFeature> copy)
    {
        _copyFeature[typeof(T)] = copy;
    }

    public static IFeature StandardCopy(IFeature feature)
    {
        if (feature is PointFeature pointFeature)
        {
            return new PointFeature(pointFeature);
        }

        if (feature is RectFeature rectFeature)
        {
            return new RectFeature(rectFeature);
        }

        if (feature is RasterFeature rasterFeature)
        {
            return new RasterFeature(rasterFeature);
        }

        if (_copyFeature.TryGetValue(feature.GetType(), out var registeredCopy))
        {
            return registeredCopy(feature);
        }
        
        throw new NotImplementedException();
    }

    public static T Copy<T>(this T original, Func<T, T>? copy = null) where T : IFeature
    {
        if (copy == null)
        {
            return (T)DefaultCopy(original);
        }
        
        return copy(original);
    }

    public static IFeature Copy(this IFeature original, Func<IFeature, IFeature>? copy = null)
    {
        copy ??= DefaultCopy;
        return copy(original);
    }

    public static IEnumerable<IFeature> Copy(this IEnumerable<IFeature> original, Func<IFeature, IFeature>? copy = null)
    {
        return original.Select(f => f.Copy(copy)).ToList();
    }

    public static string ToDisplayText(this IFeature feature)
    {
        var result = new StringBuilder();
        foreach (var field in feature.Fields)
            result.Append($"{field}:{feature[field]}");
        return result.ToString();
    }

    public static string ToDisplayText(this IEnumerable<KeyValuePair<string, IEnumerable<IFeature>>> featureInfos)
    {
        var result = new StringBuilder();

        foreach (var layer in featureInfos)
        {
            result.Append(layer.Key);
            result.Append(Environment.NewLine);
            foreach (var feature in layer.Value)
            {
                result.Append(feature.ToDisplayText());
            }
            result.Append(Environment.NewLine);
        }
        return result.ToString();
    }

    public static string ToStringOfKeyValuePairs(this IFeature feature)
    {
        var stringBuilder = new StringBuilder();
        foreach (var field in feature.Fields)
            stringBuilder.Append($"{field}: {feature[field]}\n");
        return stringBuilder.ToString();
    }


    public static IEnumerable<IFeature> Project(this IEnumerable<IFeature> features, string? fromCRS,
        string? toCRS, IProjection? projection = null, Func<IFeature, IFeature>? copy = null)
    {
        if (!CrsHelper.IsProjectionNeeded(fromCRS, toCRS))
            return features;

        if (!CrsHelper.IsCrsProvided(fromCRS, toCRS))
            throw new NotSupportedException($"CRS is not provided. From CRS: {fromCRS}. To CRS {toCRS}");

        var result = features.Copy().ToList();
        (projection ?? ProjectionDefaults.Projection).Project(fromCRS, toCRS, result);
        return result;
    }

    public static MRect? GetExtent(this IEnumerable<IFeature> features)
    {
        MRect? result = null;
        foreach (var feature in features)
        {
            if (feature.Extent is null) continue;
            result = result is null ? new MRect(feature.Extent) : result.Join(feature.Extent);
        }
        return result;
    }
}
