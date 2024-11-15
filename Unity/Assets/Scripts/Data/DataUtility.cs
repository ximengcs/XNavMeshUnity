using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

namespace XFrame.PathFinding
{
    public class DataUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int Id;
            public float X;
            public float Y;
            public int EdgeId;

            public HalfEdgeVertex ToHalfEdgeData()
            {
                HalfEdgeVertex vert = new HalfEdgeVertex(new XVector2(X, Y));
                return vert;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Edge
        {
            public int Id;
            public int PointId;
            public int FaceId;
            public int NextId;
            public int PrevId;
            public int OppositeId;

            public HalfEdge ToHalfEdgeData(HalfEdgeVertex vert)
            {
                HalfEdge edge = new HalfEdge(vert);
                return edge;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Face
        {
            public int Id;
            public int EdgeId;
            public int AreaType;

            public HalfEdgeFace ToHalfEdgeData(HalfEdge edge, AreaType area)
            {
                HalfEdgeFace face = new HalfEdgeFace(edge);
                face.Area = area;
                return face;
            }
        }

        private static int WriteStruct<T>(MemoryStream ms, T data) where T : struct
        {
            int infoSize = Marshal.SizeOf<T>();
            IntPtr p = Marshal.AllocHGlobal(infoSize);
            byte[] buffer = new byte[infoSize];
            Marshal.StructureToPtr(data, p, false);
            Marshal.Copy(p, buffer, 0, infoSize);
            ms.Write(buffer, 0, infoSize);
            Marshal.FreeHGlobal(p);
            return infoSize;
        }

        private static int ReadStruct<T>(byte[] buffer, int offset, out T data) where T : struct
        {
            int infoSize = Marshal.SizeOf<T>();
            IntPtr p = Marshal.AllocHGlobal(infoSize);
            Marshal.Copy(buffer, offset, p, infoSize);
            data = Marshal.PtrToStructure<T>(p);
            Marshal.FreeHGlobal(p);
            return offset + infoSize;
        }

        public static void Save(string name, HalfEdgeData data)
        {
            byte[] bytes = DataUtility.ToBytes(data);
            File.WriteAllBytes($"Assets/Data/{name}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.bytes", bytes);
            AssetDatabase.Refresh();
            Debug.Log($"save success, size {bytes.Length}");
        }

        public static byte[] ToBytes(XNavMesh navmesh)
        {
            using (MemoryStream ms = InnerToBytes(navmesh.Data))
            {
                ms.Write(BitConverter.GetBytes(navmesh.AABB.Min.X));
                ms.Write(BitConverter.GetBytes(navmesh.AABB.Min.Y));
                ms.Write(BitConverter.GetBytes(navmesh.AABB.Max.X));
                ms.Write(BitConverter.GetBytes(navmesh.AABB.Max.Y));

                int count = navmesh.Polies.Count;
                ms.Write(BitConverter.GetBytes(count));
                foreach (var entry in navmesh.Polies)
                {
                    Poly poly = entry.Value;
                    ms.Write(BitConverter.GetBytes(poly.Id));
                    ms.Write(BitConverter.GetBytes((int)poly.AreaType));
                    List<XVector2> polyPoints = poly.Points;
                    ms.Write(BitConverter.GetBytes(polyPoints.Count));
                    foreach (XVector2 point in polyPoints)
                    {
                        ms.Write(BitConverter.GetBytes(point.X));
                        ms.Write(BitConverter.GetBytes(point.Y));
                    }
                    ms.Write(BitConverter.GetBytes(poly.FaceCount));
                    foreach (HalfEdgeFace face in poly.Faces)
                    {
                        ms.Write(BitConverter.GetBytes(face.GetHashCode()));
                    }
                }

                return ms.ToArray();
            }
        }

        public static byte[] ToBytes(HalfEdgeData data)
        {
            using (MemoryStream ms = InnerToBytes(data))
            {
                return ms.ToArray();
            }
        }

        private static MemoryStream InnerToBytes(HalfEdgeData data)
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(data.Vertices.Count));
            foreach (HalfEdgeVertex vertex in data.Vertices)
            {
                Point point = new Point();
                point.Id = vertex.GetHashCode();
                point.X = vertex.Position.X;
                point.Y = vertex.Position.Y;
                point.EdgeId = vertex.Edge.GetHashCode();
                WriteStruct(ms, point);
            }

            ms.Write(BitConverter.GetBytes(data.Edges.Count));
            foreach (HalfEdge edge in data.Edges)
            {
                Edge e = new Edge();
                e.Id = edge.GetHashCode();
                e.PointId = edge.Vertex.GetHashCode();
                e.NextId = edge.NextEdge.GetHashCode();
                e.PrevId = edge.PrevEdge.GetHashCode();
                e.FaceId = edge.Face.GetHashCode();
                if (edge.OppositeEdge != null)
                    e.OppositeId = edge.OppositeEdge.GetHashCode();
                else
                    e.OppositeId = 0;
                WriteStruct(ms, e);
            }

            ms.Write(BitConverter.GetBytes(data.Faces.Count));
            foreach (HalfEdgeFace face in data.Faces)
            {
                Face f = new Face();
                f.Id = face.GetHashCode();
                f.EdgeId = face.Edge.GetHashCode();
                f.AreaType = (int)face.Area;
                WriteStruct(ms, f);
            }
            return ms;
        }

        public static XNavMesh ToNavmesh(byte[] bytes)
        {
            int pos = InnerFromBytes(bytes, out Dictionary<int, HalfEdgeFace> map, out HalfEdgeData data);

            float minx = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float miny = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float maxx = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float maxy = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            XNavMesh navmesh = new XNavMesh(new AABB(minx, maxx, miny, maxy), data);

            int polyCount = BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
            Dictionary<int, Poly> polies = new Dictionary<int, Poly>();
            for (int i = 0; i < polyCount; i++)
            {
                int id = BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
                AreaType areaType = (AreaType)BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
                int pointsCount = BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
                List<XVector2> polyPoints = new List<XVector2>(pointsCount);
                for (int j = 0; j < pointsCount; j++)
                {
                    float x = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
                    float y = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
                    polyPoints.Add(new XVector2(x, y));
                }

                HashSet<HalfEdgeFace> faces = new HashSet<HalfEdgeFace>();
                int faceCount = BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
                for (int j = 0; j < faceCount; j++)
                {
                    int hashCode = BitConverter.ToInt32(bytes, pos); pos += sizeof(int);
                    faces.Add(map[hashCode]);
                }

                Poly poly = new Poly(id, navmesh, polyPoints, faces, areaType);
                polies.Add(id, poly);
            }
            navmesh.Polies = polies;
            return navmesh;
        }

        public static HalfEdgeData FromBytes(byte[] bytes)
        {
            InnerFromBytes(bytes, out Dictionary<int, HalfEdgeFace> map, out HalfEdgeData data);
            return data;
        }

        private static int InnerFromBytes(byte[] bytes, out Dictionary<int, HalfEdgeFace> faces, out HalfEdgeData data)
        {
            data = new HalfEdgeData();
            Dictionary<int, HalfEdgeVertex> vertes = new Dictionary<int, HalfEdgeVertex>();
            Dictionary<int, HalfEdge> edges = new Dictionary<int, HalfEdge>();
            faces = new Dictionary<int, HalfEdgeFace>();

            int pos = 0;
            int count = BitConverter.ToInt32(bytes, pos);
            List<Point> pointDatas = new List<Point>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<Point>(bytes, pos, out Point point);
                HalfEdgeVertex v = point.ToHalfEdgeData();
                data.Vertices.Add(v);
                vertes.Add(point.Id, v);
                pointDatas.Add(point);
            }

            count = BitConverter.ToInt32(bytes, pos);
            List<Edge> edgeDatas = new List<Edge>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<Edge>(bytes, pos, out Edge edge);
                HalfEdge e = edge.ToHalfEdgeData(vertes[edge.PointId]);
                data.Edges.Add(e);
                edges.Add(edge.Id, e);
                edgeDatas.Add(edge);
            }

            count = BitConverter.ToInt32(bytes, pos);
            List<Face> faceDatas = new List<Face>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<Face>(bytes, pos, out Face face);
                HalfEdgeFace f = face.ToHalfEdgeData(edges[face.EdgeId], (AreaType)face.AreaType);
                data.Faces.Add(f);
                faces.Add(face.Id, f);
                faceDatas.Add(face);
            }

            foreach (Point point in pointDatas)
            {
                HalfEdgeVertex vert = vertes[point.Id];
                vert.Edge = edges[point.EdgeId];
            }
            foreach (Edge edge in edgeDatas)
            {
                HalfEdge e = edges[edge.Id];
                e.Face = faces[edge.FaceId];
                e.NextEdge = edges[edge.NextId];
                e.PrevEdge = edges[edge.PrevId];
                if (edge.OppositeId != 0)
                    e.OppositeEdge = edges[edge.OppositeId];
            }

            return pos;
        }
    }
}
