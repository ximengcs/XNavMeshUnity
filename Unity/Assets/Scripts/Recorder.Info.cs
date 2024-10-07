
using System;
using System.Collections.Generic;
using System.Text;
using XFrame.PathFinding;

public static partial class Recorder
{
    public class PolyFrame
    {
        public int Id;
        public List<XVector2> Points;
        public List<Triangle> Faces;

        public PolyFrame(Poly poly)
        {
            Id = poly.Id;
            Points = new List<XVector2>(poly.Points);
            Faces = new List<Triangle>();
            foreach (HalfEdgeFace face in poly.Faces)
                Faces.Add(new Triangle(face));
        }

        public override string ToString()
        {
            Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id {Id}");
            sb.AppendLine($"Points ~~~~~~~~ {Points.Count}");
            foreach (XVector2 p in Points)
                sb.AppendLine($" {(p)} ");
            sb.AppendLine("++++++++++++++++");
            sb.AppendLine($"Faces ~~~~~~~~ {Faces.Count}");
            foreach (Triangle tri in Faces)
                sb.AppendLine($" <{f(tri.P1)} {f(tri.P2)} {f(tri.P3)}> ");
            sb.AppendLine("++++++++++++++++");
            return sb.ToString();
        }
    }

    public class Info
    {
        public int PolyId;
        public List<XVector2> OldPoints;
        public List<XVector2> NewPoints;
        public List<Triangle> RelationFaces;
        public List<XVector2> NewAreaOutEdges;
        public List<PolyFrame> Polies;
        public List<List<XVector2>> RelationAllPoints;
        public Dictionary<int, List<XVector2>> PolyNewPoints;
        public List<Triangle> NewHalfEdgeData;
        public HalfEdgeData CloneData;

        public override string ToString()
        {
            Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PolyId  {PolyId}");
            sb.AppendLine($"OldPoints ========================= {OldPoints.Count}");
            foreach (XVector2 p in OldPoints)
                sb.AppendLine($" {f(p)} ");
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"NewPoints ========================= {NewPoints.Count}");
            foreach (XVector2 p in NewPoints)
                sb.AppendLine($" {f(p)} ");
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"RelationFaces ========================= {RelationFaces.Count}");
            foreach (Triangle tri in RelationFaces)
                sb.AppendLine($" <{f(tri.P1)} {f(tri.P2)} {f(tri.P3)}> ");
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"NewAreaOutEdges ========================= {NewAreaOutEdges.Count}");
            foreach (XVector2 p in NewAreaOutEdges)
                sb.AppendLine($" {f(p)} ");
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"Polies ========================= {Polies.Count}");
            foreach (PolyFrame p in Polies)
            {
                sb.AppendLine($"PolyFrame ======");
                sb.Append(p);
            }
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"RelationAllPoints ========================= {RelationAllPoints.Count}");
            foreach (List<XVector2> list in RelationAllPoints)
            {
                sb.AppendLine($"====== {list.Count}");
                foreach (XVector2 p in list)
                    sb.AppendLine($" {f(p)} ");
            }
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"PolyNewPoints ========================= {PolyNewPoints.Count}");
            foreach (var item in PolyNewPoints)
            {
                sb.AppendLine($"poly -> {item.Key} ~~~~~~~~~~");
                foreach (XVector2 p in item.Value)
                    sb.AppendLine($" {f(p)} ");
            }
            sb.AppendLine($"-----------------------------------------------------");

            sb.AppendLine($"NewHalfEdgeData ========================= {NewHalfEdgeData.Count}");
            foreach (Triangle tri in NewHalfEdgeData)
                sb.AppendLine($" <{f(tri.P1)} {tri.P2} {tri.P3}> ");
            sb.AppendLine($"-----------------------------------------------------");

            return sb.ToString();
        }

        public Info()
        {
            OldPoints = new List<XVector2>();
            NewPoints = new List<XVector2>();
            RelationFaces = new List<Triangle>();
            NewAreaOutEdges = new List<XVector2>();
            Polies = new List<PolyFrame>();
            RelationAllPoints = new List<List<XVector2>>();
            NewHalfEdgeData = new List<Triangle>();
            PolyNewPoints = new Dictionary<int, List<XVector2>>();
        }

        public void SetOldPoints(List<XVector2> oldPoints)
        {
            OldPoints.AddRange(oldPoints);
        }

        public void SetNewPoints(List<XVector2> newPoints)
        {
            NewPoints.AddRange(newPoints);
        }

        public void SetRelationFaces(HashSet<HalfEdgeFace> faces)
        {
            foreach (HalfEdgeFace face in faces)
                RelationFaces.Add(new Triangle(face));
        }

        public void SetNewAreaOutEdges(List<Edge> newAreaOutEdges)
        {
            foreach (Edge edge in newAreaOutEdges)
                NewAreaOutEdges.Add(edge.P1);
        }

        public void SetPolies(Dictionary<int, Poly> polies)
        {
            foreach (var item in polies)
            {
                PolyFrame frame = new PolyFrame(item.Value);
                Polies.Add(frame);
            }
        }

        public void SetRelationNewPoint(Dictionary<Poly, List<XVector2>> list)
        {
            foreach (var item in list)
            {
                PolyNewPoints.Add(item.Key.Id, new List<XVector2>(item.Value));
            }
        }

        public void SetRelationAllPoints(List<List<XVector2>> relationAllPoints)
        {
            foreach (List<XVector2> item in relationAllPoints)
            {
                List<XVector2> copy = new List<XVector2>(item);
                RelationAllPoints.Add(copy);
            }
        }

        public void SetHalfEdgeData(HalfEdgeData data)
        {
            foreach (HalfEdgeFace face in data.Faces)
            {
                NewHalfEdgeData.Add(new Triangle(face));
            }
        }

        public void Dispose()
        {
            OldPoints.Clear();
            NewPoints.Clear();
            RelationFaces.Clear();
            NewAreaOutEdges.Clear();
            Polies.Clear();
            RelationAllPoints.Clear();
            NewHalfEdgeData.Clear();
        }
    }
}
