using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Injaia
{
    [System.Serializable]
    public class MarkerPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public MarkerPoint(Vector3 _position, Quaternion _rotation)
        {
            Position = _position;
            Rotation = _rotation;
        }

        public float DistSquared2D(Transform referencePoint)
        {
            return (Position.x - referencePoint.position.x)*(Position.x - referencePoint.position.x) +
                   (Position.z - referencePoint.position.z)*(Position.z - referencePoint.position.z);
        }
    }

    [CreateAssetMenu(fileName = "PathSO", menuName = "Injaia/Path", order = 1)]
    public class PathSO : ScriptableObject
    {
        public List<MarkerPoint> Markers = new List<MarkerPoint>();

        public void StartPath()
        {
            Markers = new List<MarkerPoint>();
        }

        public void EndPath()
        {
        }

        public void AddPathPoint(Vector3 position, Quaternion rotation)
        {
            if (Markers == null)
                Markers = new List<MarkerPoint>();
                
            Markers.Add(new MarkerPoint(position, rotation));
        }

        public MarkerPoint GetClosestMarker2D(Transform referencePoint)
        {
            return Markers.OrderBy(marker => marker.DistSquared2D(referencePoint)).FirstOrDefault();
        }
    }
}
