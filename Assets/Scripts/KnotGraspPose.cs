using UnityEngine;

/// <summary>Fits the visible opening of the red knot ring in a single wrist image.</summary>
public static class KnotGraspPose
{
    public const float OuterMajorDiameter = .060f;
    public const float OuterMinorDiameter = .050f;
    public const float CordDiameter = .009f;
    const int SampleSize = 192;
    const int RimSamples = 64;
    const float MajorRadius = (OuterMajorDiameter - CordDiameter) / 2f;
    const float MinorRadius = (OuterMinorDiameter - CordDiameter) / 2f;

    public static bool TryEstimate(Texture2D image, Vector2 tcpUv, Quaternion imageToTcp, out Pose pose)
    {
        pose = default;
        float sampleScale = (float)SampleSize / Mathf.Max(image.width, image.height);
        int width = Mathf.RoundToInt(image.width * sampleScale);
        int height = Mathf.RoundToInt(image.height * sampleScale);
        if (width < 16 || height < 16) return false;

        var pixels = image.GetPixels32();
        var red = new bool[width * height];
        for (int y = 0; y < height; y++)
        {
            int row = Mathf.Min((int)((y + .5f) * image.height / height), image.height - 1) * image.width;
            for (int x = 0; x < width; x++)
            {
                var color = pixels[row + Mathf.Min((int)((x + .5f) * image.width / width), image.width - 1)];
                red[y * width + x] = color.r > 60 && color.r - color.g > 24 && color.r - color.b > 20 &&
                    3 * color.r > 4 * color.g && 3 * color.r > 4 * color.b;
            }
        }

        // Close narrow breaks in the rope mask. Score the fit against the original pixels.
        bool[] visited = FilterMask(FilterMask(red, width, height, true), width, height, false);
        var component = new int[visited.Length];
        var anchor = new Vector2(Mathf.Clamp01(tcpUv.x) * width - .5f, Mathf.Clamp01(tcpUv.y) * height - .5f);
        float bestScore = float.NegativeInfinity;
        float runnerUp = float.NegativeInfinity;
        for (int i = 0; i < visited.Length; i++)
        {
            if (visited[i]) continue;
            int count = FloodOpening(i, visited, width, height, component, out bool touchesEdge);
            if (touchesEdge || count < 80) continue;
            if (!TryFit(component, count, red, width, height, anchor, imageToTcp, out Pose estimate, out float score))
                continue;
            if (score > bestScore)
            {
                runnerUp = bestScore;
                bestScore = score;
                pose = estimate;
            }
            else runnerUp = Mathf.Max(runnerUp, score);
        }
        return bestScore > float.NegativeInfinity && bestScore - runnerUp >= .08f;
    }

    static bool[] FilterMask(bool[] source, int width, int height, bool dilate)
    {
        var result = new bool[source.Length];
        for (int y = 1; y < height - 1; y++)
        for (int x = 1; x < width - 1; x++)
        {
            int i = y * width + x;
            result[i] = dilate
                ? source[i] || source[i - 1] || source[i + 1] || source[i - width] || source[i + width]
                : source[i] && source[i - 1] && source[i + 1] && source[i - width] && source[i + width];
        }
        return result;
    }

    static int FloodOpening(int seed, bool[] visited, int width, int height, int[] component, out bool touchesEdge)
    {
        int count = 0;
        touchesEdge = false;
        Add(seed);
        for (int head = 0; head < count; head++)
        {
            int index = component[head];
            int x = index % width;
            int y = index / width;
            touchesEdge |= x == 0 || x == width - 1 || y == 0 || y == height - 1;
            if (x > 0) Add(index - 1);
            if (x < width - 1) Add(index + 1);
            if (y > 0) Add(index - width);
            if (y < height - 1) Add(index + width);
        }
        return count;

        void Add(int index)
        {
            if (visited[index]) return;
            visited[index] = true;
            component[count++] = index;
        }
    }

    static bool TryFit(int[] component, int count, bool[] red, int width, int height, Vector2 tcp,
        Quaternion imageToTcp, out Pose pose, out float score)
    {
        pose = default;
        score = 0f;
        float x = 0, y = 0, xx = 0, xy = 0, yy = 0;
        for (int i = 0; i < count; i++)
        {
            int px = component[i] % width;
            int py = component[i] / width;
            x += px;
            y += py;
            xx += px * px;
            xy += px * py;
            yy += py * py;
        }
        var center = new Vector2(x / count, y / count);
        float varX = xx / count - center.x * center.x;
        float varY = yy / count - center.y * center.y;
        float cov = xy / count - center.x * center.y;
        float radius = Mathf.Sqrt((varX - varY) * (varX - varY) + 4f * cov * cov);
        float major = Mathf.Sqrt(2f * (varX + varY + radius));
        float minor = Mathf.Sqrt(2f * (varX + varY - radius));
        if (minor < 5f || minor < .35f * major || count < .78f * Mathf.PI * major * minor) return false;

        float angle = .5f * Mathf.Atan2(2f * cov, varX - varY);
        var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        var cross = new Vector2(axis.y, -axis.x);
        float innerMajor = MajorRadius - CordDiameter / 2f;
        float innerMinor = MinorRadius - CordDiameter / 2f;
        Vector2 fromCenter = tcp - center;
        var contact = new Vector2(Vector2.Dot(fromCenter, cross) * innerMinor / minor,
            Vector2.Dot(fromCenter, axis) * innerMajor / major);

        int supportedSamples = 0;
        float nearestSquared = float.PositiveInfinity;
        Vector2 rimContact = default;
        for (int i = 0; i < RimSamples; i++)
        {
            float phase = i * (2f * Mathf.PI / RimSamples);
            float sin = Mathf.Sin(phase), cos = Mathf.Cos(phase);
            var rim = new Vector2(MinorRadius * cos, MajorRadius * sin);
            float distance = (rim - contact).sqrMagnitude;
            if (distance < nearestSquared)
            {
                rimContact = rim;
                nearestSquared = distance;
            }
            var ray = cross * (minor * cos) + axis * (major * sin);
            if (IsRed(center + 1.05f * ray) || IsRed(center + 1.18f * ray) || IsRed(center + 1.34f * ray))
                supportedSamples++;
        }

        float support = (float)supportedSamples / RimSamples;
        float contactError = Mathf.Sqrt(nearestSquared);
        if (support < .70f || contactError > .014f) return false;

        Vector3 up = imageToTcp * new Vector3(axis.x, axis.y, 0f);
        Quaternion rotation = Quaternion.LookRotation(imageToTcp * Vector3.forward, up);
        pose = new Pose(rotation * new Vector3(-rimContact.x, -rimContact.y, 0f), rotation);
        score = support - contactError / .024f;
        return true;

        bool IsRed(Vector2 point)
        {
            int px = Mathf.RoundToInt(point.x), py = Mathf.RoundToInt(point.y);
            return px >= 0 && px < width && py >= 0 && py < height && red[py * width + px];
        }
    }
}
