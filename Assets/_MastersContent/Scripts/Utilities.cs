using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class Utilities
{
    public static float LimitAxis(float limit, float value)
    {
        float newValue = value;

        if (value > limit)
            newValue = limit;
        else if (value < -limit)
            newValue = -limit;

        return newValue;
    }

    public static Vector3 LimitAxis(Vector3 limit, Vector3 value)
    {
        Vector3 newValue = value;

        if (value.x > limit.x)
            newValue = new Vector3(limit.x, newValue.y, newValue.z);
        else if (value.x < -limit.x)
            newValue = new Vector3(-limit.x, newValue.y, newValue.z);

        if (value.y > limit.y)
            newValue = new Vector3(newValue.x, limit.y, newValue.z);
        else if (value.y < -limit.y)
            newValue = new Vector3(newValue.x, -limit.y, newValue.z);

        if (value.z > limit.z)
            newValue = new Vector3(newValue.x, newValue.y, limit.z);
        else if (value.z < -limit.z)
            newValue = new Vector3(newValue.x, newValue.y, -limit.z);

        return newValue;
    }

    public static float Map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = false)
    {
        if (clamp) x = Mathf.Max(in_min, Mathf.Min(x, in_max));
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    public static bool IsInside(Collider collider, Vector3 point)
    {
        Vector3 closest = collider.ClosestPoint(point);
        return closest == point;
    }


    public static float StandardDeviation(this IEnumerable<float> values)
    {
        float avg = values.Average();
        return Mathf.Sqrt(values.Average(v => Mathf.Pow(v - avg, 2)));
    }


    public static bool PointInOABB(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;

        float halfX = (box.size.x * 0.5f);
        float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);
        if (point.x < halfX && point.x > -halfX &&
           point.y < halfY && point.y > -halfY &&
           point.z < halfZ && point.z > -halfZ)
            return true;
        else
            return false;
    }

    public static bool IsWithinLocalBounds(Vector3 point, Vector3 scale)
    {
        if (point.x < -scale.x * 0.5f && point.x < scale.x * 0.5f &&
            point.y < -scale.y * 0.5f && point.y < scale.y * 0.5f &&
            point.z < -scale.z * 0.5f && point.z < scale.z * 0.5f)
        return true;
        else
            return false;
    }
}