using System.Globalization;

namespace storage_sqllite;

public static class Extensions
{
    public static double[] ToDoubleArray(this string str)
    {
        var splits = str.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return splits.Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
    }
}