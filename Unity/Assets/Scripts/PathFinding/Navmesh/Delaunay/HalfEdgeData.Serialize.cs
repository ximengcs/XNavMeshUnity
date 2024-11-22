
using System.Drawing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public partial class HalfEdgeData
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct PointData
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
        private struct EdgeData
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
        private struct FaceData
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

        internal static int InnerFromBytes(HalfEdgeData data, byte[] bytes, out Dictionary<int, HalfEdgeFace> faces)
        {
            Dictionary<int, HalfEdgeVertex> vertes = new Dictionary<int, HalfEdgeVertex>();
            Dictionary<int, HalfEdge> edges = new Dictionary<int, HalfEdge>();
            faces = new Dictionary<int, HalfEdgeFace>();

            int pos = 0;
            int count = BitConverter.ToInt32(bytes, pos);
            List<PointData> pointDatas = new List<PointData>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<PointData>(bytes, pos, out PointData point);
                HalfEdgeVertex v = point.ToHalfEdgeData();
                data.Vertices.Add(v);
                vertes.Add(point.Id, v);
                pointDatas.Add(point);
            }

            count = BitConverter.ToInt32(bytes, pos);
            List<EdgeData> edgeDatas = new List<EdgeData>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<EdgeData>(bytes, pos, out EdgeData edge);
                HalfEdge e = edge.ToHalfEdgeData(vertes[edge.PointId]);
                data.Edges.Add(e);
                edges.Add(edge.Id, e);
                edgeDatas.Add(edge);
            }

            count = BitConverter.ToInt32(bytes, pos);
            List<FaceData> faceDatas = new List<FaceData>(count);
            pos += sizeof(int);
            for (int i = 0; i < count; i++)
            {
                pos = ReadStruct<FaceData>(bytes, pos, out FaceData face);
                HalfEdgeFace f = face.ToHalfEdgeData(edges[face.EdgeId], (AreaType)face.AreaType);
                data.Faces.Add(f);
                faces.Add(face.Id, f);
                faceDatas.Add(face);
            }

            foreach (PointData point in pointDatas)
            {
                HalfEdgeVertex vert = vertes[point.Id];
                vert.Edge = edges[point.EdgeId];
            }
            foreach (EdgeData edge in edgeDatas)
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

        public byte[] ToBytes()
        {
            using (MemoryStream ms = InnerToBytes())
            {
                return ms.ToArray();
            }
        }

        internal MemoryStream InnerToBytes()
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(Vertices.Count));
            foreach (HalfEdgeVertex vertex in Vertices)
            {
                PointData point = new PointData();
                point.Id = vertex.GetHashCode();
                point.X = vertex.Position.X;
                point.Y = vertex.Position.Y;
                point.EdgeId = vertex.Edge.GetHashCode();
                WriteStruct(ms, point);
            }

            ms.Write(BitConverter.GetBytes(Edges.Count));
            foreach (HalfEdge edge in Edges)
            {
                EdgeData e = new EdgeData();
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

            ms.Write(BitConverter.GetBytes(Faces.Count));
            foreach (HalfEdgeFace face in Faces)
            {
                FaceData f = new FaceData();
                f.Id = face.GetHashCode();
                f.EdgeId = face.Edge.GetHashCode();
                f.AreaType = (int)face.Area;
                WriteStruct(ms, f);
            }
            return ms;
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
    }
}
