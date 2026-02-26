using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    public Transform[] Points;

    void OnValidate()
    {
        // Auto-fill children if you forget to assign
        if (Points == null || Points.Length == 0)
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in transform) list.Add(t);
            Points = list.ToArray();
        }
    }

    void OnDrawGizmos()
    {
        if (Points == null || Points.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < Points.Length - 1; i++)
        {
            if (!Points[i] || !Points[i + 1]) continue;
            Gizmos.DrawSphere(Points[i].position, 0.2f);
            Gizmos.DrawLine(Points[i].position, Points[i + 1].position);
        }
        if (Points[Points.Length - 1])
            Gizmos.DrawSphere(Points[Points.Length - 1].position, 0.2f);
    }
}
