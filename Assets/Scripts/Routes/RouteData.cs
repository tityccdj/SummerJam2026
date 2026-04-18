using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoutePointData
{
    public float x;
    public float y;

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }
}

[Serializable]
public class RouteData
{
    public int routeId = 1;
    public string displayName = "Route";
    public bool closed = true;
    public float trackWidth = 2.5f;
    public float trackScale = 3.0f;
    public int subdivisionsPerSegment = 10;
    public RoutePointData[] points = Array.Empty<RoutePointData>();

    public bool IsValid()
    {
        return points != null && points.Length >= 2;
    }

    public Vector3[] GetSplinePoints()
    {
        if (!IsValid())
        {
            return Array.Empty<Vector3>();
        }

        int subdivisions = Mathf.Max(1, subdivisionsPerSegment);
        List<Vector3> sampledPoints = new List<Vector3>();
        int pointCount = points.Length;
        int segmentCount = closed ? pointCount : pointCount - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 p0 = GetPoint(i - 1, closed);
            Vector2 p1 = GetPoint(i, closed);
            Vector2 p2 = GetPoint(i + 1, closed);
            Vector2 p3 = GetPoint(i + 2, closed);

            for (int step = 0; step < subdivisions; step++)
            {
                float t = step / (float)subdivisions;
                Vector2 sampled = EvaluateCatmullRom(p0, p1, p2, p3, t);
                sampledPoints.Add(new Vector3(sampled.x, sampled.y, 0f));
            }
        }

        if (!closed)
        {
            Vector2 endPoint = points[pointCount - 1].ToVector2();
            sampledPoints.Add(new Vector3(endPoint.x, endPoint.y, 0f));
        }

        return sampledPoints.ToArray();
    }

    public Vector2 EvaluatePosition(float progress)
    {
        if (!IsValid())
        {
            return Vector2.zero;
        }

        int pointCount = points.Length;
        int segmentCount = closed ? pointCount : pointCount - 1;
        float normalizedProgress = closed ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);
        float scaledProgress = normalizedProgress * segmentCount;
        int segmentIndex = Mathf.FloorToInt(scaledProgress);
        float segmentT = scaledProgress - segmentIndex;

        if (!closed && segmentIndex >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            segmentT = 1f;
        }

        Vector2 p0 = GetPoint(segmentIndex - 1, closed);
        Vector2 p1 = GetPoint(segmentIndex, closed);
        Vector2 p2 = GetPoint(segmentIndex + 1, closed);
        Vector2 p3 = GetPoint(segmentIndex + 2, closed);
        return EvaluateCatmullRom(p0, p1, p2, p3, segmentT);
    }

    public Vector2 EvaluateTangent(float progress)
    {
        if (!IsValid())
        {
            return Vector2.right;
        }

        int pointCount = points.Length;
        int segmentCount = closed ? pointCount : pointCount - 1;
        float normalizedProgress = closed ? Mathf.Repeat(progress, 1f) : Mathf.Clamp01(progress);
        float scaledProgress = normalizedProgress * segmentCount;
        int segmentIndex = Mathf.FloorToInt(scaledProgress);
        float segmentT = scaledProgress - segmentIndex;

        if (!closed && segmentIndex >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            segmentT = 1f;
        }

        Vector2 p0 = GetPoint(segmentIndex - 1, closed);
        Vector2 p1 = GetPoint(segmentIndex, closed);
        Vector2 p2 = GetPoint(segmentIndex + 1, closed);
        Vector2 p3 = GetPoint(segmentIndex + 2, closed);

        Vector2 tangent = EvaluateCatmullRomTangent(p0, p1, p2, p3, segmentT);
        return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector2.right;
    }

    public Vector2 EvaluateNormal(float progress)
    {
        Vector2 tangent = EvaluateTangent(progress);
        return new Vector2(-tangent.y, tangent.x);
    }

    private Vector2 GetPoint(int index, bool loop)
    {
        //int count = points.Length;

        //if (loop)
        //{
        //    int wrappedIndex = (index % count + count) % count;
        //    return points[wrappedIndex].ToVector2();
        //}

        //int clampedIndex = Mathf.Clamp(index, 0, count - 1);
        //return points[clampedIndex].ToVector2();
        int count = points.Length;
        Vector2 rawPoint;

        if (loop)
        {
            int wrappedIndex = (index % count + count) % count;
            rawPoint = points[wrappedIndex].ToVector2();
        }
        else
        {
            int clampedIndex = Mathf.Clamp(index, 0, count - 1);
            rawPoint = points[clampedIndex].ToVector2();
        }

        // --- คูณด้วย trackScale ก่อนส่งออกไปใช้งาน ---
        return rawPoint * trackScale;
    }

    private static Vector2 EvaluateCatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static Vector2 EvaluateCatmullRomTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;

        return 0.5f * (
            (-p0 + p2) +
            2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * t +
            3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2
        );
    }
}
