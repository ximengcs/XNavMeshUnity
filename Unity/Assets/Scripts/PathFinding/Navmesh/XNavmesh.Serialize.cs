using System;
using System.IO;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public partial class XNavmesh
    {
        private void InnerToNavmesh(byte[] bytes)
        {
            m_Data = new HalfEdgeData();
            int pos = HalfEdgeData.InnerFromBytes(m_Data, bytes, out Dictionary<int, HalfEdgeFace> map);

            float minx = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float miny = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float maxx = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            float maxy = BitConverter.ToSingle(bytes, pos); pos += sizeof(float);
            m_AABB = new AABB(minx, maxx, miny, maxy);
            m_Normalizer = new Normalizer(m_AABB);

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

                Poly poly = new Poly(id, this, polyPoints, faces, areaType);
                polies.Add(id, poly);
            }
            m_Polies = polies;
        }

        public byte[] ToBytes()
        {
            using (MemoryStream ms = m_Data.InnerToBytes())
            {
                ms.Write(BitConverter.GetBytes(Min.X));
                ms.Write(BitConverter.GetBytes(Min.Y));
                ms.Write(BitConverter.GetBytes(Max.X));
                ms.Write(BitConverter.GetBytes(Max.Y));

                int count = m_Polies.Count;
                ms.Write(BitConverter.GetBytes(count));
                foreach (var entry in m_Polies)
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
    }
}
