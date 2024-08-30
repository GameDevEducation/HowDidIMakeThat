using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vector2IntExtensions
{
    public static class Vector2IntExt
    {
        public static Vector2Int Normalise(this Vector2Int self)
        {
            return new Vector2Int(self.x == 0 ? 0 : (self.x / Mathf.Abs(self.x)),
                                  self.y == 0 ? 0 : (self.y / Mathf.Abs(self.y)));
        }

        public static int Dot(this Vector2Int self, Vector2Int other)
        {
            return self.x * other.x + self.y * other.y;
        }
    }
}
