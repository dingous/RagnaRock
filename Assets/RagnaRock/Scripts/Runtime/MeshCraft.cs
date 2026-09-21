using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RagnaRock
{
    /// <summary>Original flat-shaded geometry. No imported meshes, fonts or commercial assets.</summary>
    public sealed class MeshCraft
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color> colors = new List<Color>();
        private readonly List<int> triangles = new List<int>();
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            int first = vertices.Count;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
            for (int i = 0; i < 3; i++) { normals.Add(normal); colors.Add(color); triangles.Add(first + i); }
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        { Triangle(a, b, c, color); Triangle(a, c, d, color); }
        public void Box(Vector3 center, Vector3 size, Color color, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            Vector3 h = size * .5f;
            Vector3[] p = {
                new Vector3(-h.x,-h.y,-h.z), new Vector3(h.x,-h.y,-h.z),
                new Vector3(h.x,h.y,-h.z), new Vector3(-h.x,h.y,-h.z),
                new Vector3(-h.x,-h.y,h.z), new Vector3(h.x,-h.y,h.z),
                new Vector3(h.x,h.y,h.z), new Vector3(-h.x,h.y,h.z) };
            for (int i = 0; i < 8; i++) p[i] = center + q * p[i];
            Quad(p[0], p[3], p[2], p[1], color); Quad(p[4], p[5], p[6], p[7], color);
            Quad(p[0], p[4], p[7], p[3], color); Quad(p[1], p[2], p[6], p[5], color);
            Quad(p[3], p[7], p[6], p[2], color); Quad(p[0], p[1], p[5], p[4], color);
        }
        public void Cylinder(Vector3 center, float radius, float height, Color color, int sides = 12, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 loA = new Vector3(Mathf.Cos(a) * radius, -height * .5f, Mathf.Sin(a) * radius);
                Vector3 loB = new Vector3(Mathf.Cos(b) * radius, -height * .5f, Mathf.Sin(b) * radius);
                Vector3 hiA = loA + Vector3.up * height, hiB = loB + Vector3.up * height;
                Quad(center + q * loA, center + q * hiA, center + q * hiB, center + q * loB, color);
                Triangle(center + q * (Vector3.up * height * .5f), center + q * hiB, center + q * hiA, color);
                Triangle(center - q * (Vector3.up * height * .5f), center + q * loA, center + q * loB, color);
            }
        }
        public void Cone(Vector3 center, float radius, float height, Color color, int sides = 8, Quaternion? rotation = null)
        {
            Quaternion q = rotation ?? Quaternion.identity;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                Vector3 r = new Vector3(Mathf.Cos(b) * radius, 0, Mathf.Sin(b) * radius);
                Triangle(center + q * p, center + q * (Vector3.up * height), center + q * r, color);
                Triangle(center, center + q * p, center + q * r, color);
            }
        }
        public void Octahedron(Vector3 center, Vector3 scale, Color color)
        {
            Vector3[] ring = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = center + Vector3.Scale(ring[i], scale), b = center + Vector3.Scale(ring[(i + 1) % 4], scale);
                Triangle(center + Vector3.up * scale.y, b, a, color);
                Triangle(center - Vector3.up * scale.y, a, b, color);
            }
        }
        public void Torus(Vector3 center, float radius, float tube, Color color, int segments = 48, int sides = 4)
        {
            for (int i = 0; i < segments; i++) for (int j = 0; j < sides; j++)
            {
                Vector3 a = TorusPoint(i, j, segments, sides, radius, tube) + center;
                Vector3 b = TorusPoint(i + 1, j, segments, sides, radius, tube) + center;
                Vector3 c = TorusPoint(i + 1, j + 1, segments, sides, radius, tube) + center;
                Vector3 d = TorusPoint(i, j + 1, segments, sides, radius, tube) + center;
                Quad(a, d, c, b, color);
            }
        }
        private static Vector3 TorusPoint(int i, int j, int segments, int sides, float radius, float tube)
        {
            float a = i * Mathf.PI * 2 / segments, b = j * Mathf.PI * 2 / sides;
            float r = radius + Mathf.Cos(b) * tube;
            return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(b) * tube, Mathf.Sin(a) * r);
        }
        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
