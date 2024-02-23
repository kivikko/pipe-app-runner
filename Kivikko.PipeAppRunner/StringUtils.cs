namespace Kivikko.PipeAppRunner;

public static class StringUtils
{
    // https://regex101.com/r/kV1dL6/1

    public static void Split(this string text, char separator, out string firstPart, out string secondPart)
    {
        var separatorIndex = text.IndexOf(separator);
        firstPart = separatorIndex > 0 ? text.Substring(0, separatorIndex) : text;
        secondPart = separatorIndex > 0 && separatorIndex < text.Length - 1 ? text.Substring(separatorIndex + 1) : null;
    }

    public static void Split(this string text, char separator, out string firstPart, out string secondPart, out string thirdPart)
    {
        text.Split(separator, out firstPart, out text);
        text.Split(separator, out secondPart, out thirdPart);
    }

    public static void Split(this string text, char separator, out string firstPart, out string secondPart, out string thirdPart, out string fourthPart)
    {
        text.Split(separator, out firstPart, out text);
        text.Split(separator, out secondPart, out text);
        text.Split(separator, out thirdPart, out fourthPart);
    }
}