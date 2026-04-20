using UnityEngine;

public static class Math3dExtensions
{
    // ── Vector3 ──────────────────────────────────────────────────────────────

    public static Vector3 SetLength(this Vector3 v, float size) =>
        Math3d.SetVectorLength(v, size);

    public static Vector3 AddLength(this Vector3 v, float size) =>
        Math3d.AddVectorLength(v, size);

    public static Vector3 ProjectOnLine(this Vector3 point, Vector3 linePoint, Vector3 lineDir) =>
        Math3d.ProjectPointOnLine(linePoint, lineDir, point);

    public static Vector3 ProjectOnLineSegment(this Vector3 point, Vector3 linePoint1, Vector3 linePoint2) =>
        Math3d.ProjectPointOnLineSegment(linePoint1, linePoint2, point);

    public static Vector3 ProjectOnPlane(this Vector3 point, Vector3 planeNormal, Vector3 planePoint) =>
        Math3d.ProjectPointOnPlane(planeNormal, planePoint, point);

    public static Vector3 ProjectVectorOnPlane(this Vector3 v, Vector3 planeNormal) =>
        Math3d.ProjectVectorOnPlane(planeNormal, v);

    public static float SignedDistanceTo(this Vector3 point, Vector3 planeNormal, Vector3 planePoint) =>
        Math3d.SignedDistancePlanePoint(planeNormal, planePoint, point);

    public static int SideOfLineSegment(this Vector3 point, Vector3 linePoint1, Vector3 linePoint2) =>
        Math3d.PointOnWhichSideOfLineSegment(linePoint1, linePoint2, point);

    public static float SignedAngleTo(this Vector3 reference, Vector3 other, Vector3 normal) =>
        Math3d.SignedVectorAngle(reference, other, normal);

    public static float AngleToPlane(this Vector3 v, Vector3 planeNormal) =>
        Math3d.AngleVectorPlane(v, planeNormal);

    // ── Quaternion ────────────────────────────────────────────────────────────

    public static Vector3 Forward(this Quaternion q) => Math3d.GetForwardVector(q);
    public static Vector3 Up(this Quaternion q)      => Math3d.GetUpVector(q);
    public static Vector3 Right(this Quaternion q)   => Math3d.GetRightVector(q);

    public static Quaternion Subtract(this Quaternion b, Quaternion a) =>
        Math3d.SubtractRotation(b, a);

    public static Quaternion Add(this Quaternion a, Quaternion b) =>
        Math3d.AddRotation(a, b);

    public static Vector3 TransformDir(this Quaternion rotation, Vector3 vector) =>
        Math3d.TransformDirectionMath(rotation, vector);

    public static Vector3 InverseTransformDir(this Quaternion rotation, Vector3 vector) =>
        Math3d.InverseTransformDirectionMath(rotation, vector);

    // ── Transform ─────────────────────────────────────────────────────────────

    public static void LookRotationExtended(this Transform t,
        Vector3 alignWithVector, Vector3 alignWithNormal,
        Vector3 customForward, Vector3 customUp)
    {
        var go = t.gameObject;
        Math3d.LookRotationExtended(ref go, alignWithVector, alignWithNormal, customForward, customUp);
    }

    public static void SetFromVectors(this Transform t,
        Vector3 position, Vector3 direction, Vector3 normal)
    {
        var go = t.gameObject;
        Math3d.VectorsToTransform(ref go, position, direction, normal);
    }
}
