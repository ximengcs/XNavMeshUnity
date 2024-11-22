using System.Collections.Generic;
using System;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 约束Delaunay
    /// 当没有约束时所有形状都是凸的，所以需要约束
    /// 原理：检查约束的形状的边和下一条边是否有相交的半边，如果有则翻转边
    /// </summary>
    internal class ConstrainedDelaunaySloan
    {
        /// <summary>
        /// 添加约束边
        /// </summary>
        /// <param name="triangleData"></param>
        /// <param name="constraints"></param>
        /// <param name="shouldRemoveTriangles"></param>
        /// <returns></returns>
        public static HalfEdgeData AddConstraints(HalfEdgeData triangleData, List<XVector2> constraints, bool shouldRemoveTriangles)
        {
            //Get a list with all edges
            //This is faster than first searching for unique edges
            //The report suggest we should do a triangle walk, but it will not work if the mesh has holes
            //The mesh has holes because we remove triangles while adding constraints one-by-one
            //so maybe better to remove triangles after we added all constraints...
            HashSet<HalfEdge> edges = triangleData.Edges;

            //The steps numbering is from the report
            //Step 1. Loop over each constrained edge. For each of these edges, do steps 2-4 
            for (int i = 0; i < constraints.Count; i++)
            {
                //Let each constrained edge be defined by the vertices:
                XVector2 c_p1 = constraints[i];
                XVector2 c_p2 = constraints[XMath.ClampListIndex(i + 1, constraints.Count)];

                Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
                // 检查两个点组成的边是否已在半边数据集合中，如果存在则跳过
                if (IsEdgeInListOfEdges(edges, c_p1, c_p2))
                {
                    continue;
                }

                //Step 2. Find all edges in the current triangulation that intersects with this constraint
                //Is returning unique edges only, so not one edge going in the opposite direction
                //timer.Start();
                // 找到所有与之(约束)相交的边
                Queue<HalfEdge> intersectingEdges = FindIntersectingEdges_BruteForce(edges, c_p1, c_p2);
                //timer.Stop();

                //Debug.Log("Intersecting edges: " + intersectingEdges.Count);

                //Step 3. Remove intersecting edges by flipping triangles
                //This takes 0 seconds so is not bottleneck
                //timer.Start();

                List<HalfEdge> newEdges = RemoveIntersectingEdges(c_p1, c_p2, intersectingEdges);
                //timer.Stop();

                //Step 4. Try to restore delaunay triangulation 
                //Because we have constraints we will never get a delaunay triangulation
                //This takes 0 seconds so is not bottleneck
                //timer.Start();
                RestoreDelaunayTriangulation(c_p1, c_p2, newEdges);
                //timer.Stop();
            }

            //Step 5. Remove superfluous triangles, such as the triangles "inside" the constraints  
            if (shouldRemoveTriangles)
            {
                //timer.Start();
                RemoveSuperfluousTriangles(triangleData, constraints);
                //timer.Stop();
            }

            return triangleData;
        }

        //
        // Remove all triangles that are inside the constraint
        //

        //This assumes the vertices in the constraint are ordered clockwise
        private static void RemoveSuperfluousTriangles(HalfEdgeData triangleData, List<XVector2> constraints)
        {
            //This assumes we have at least 3 vertices in the constraint because we cant delete triangles inside a line
            if (constraints.Count < 3)
            {
                return;
            }

            HashSet<HalfEdgeFace> trianglesToBeDeleted = FindTrianglesWithinConstraint(triangleData, constraints);

            if (trianglesToBeDeleted == null)
            {
                return;
            }

            //Delete the triangles
            foreach (HalfEdgeFace t in trianglesToBeDeleted)
            {
                DelaunayIncrementalSloan.DeleteTriangleFace(t, triangleData, true);
            }
        }

        //
        // Find which triangles are within a constraint
        //

        public static HashSet<HalfEdgeFace> FindTrianglesWithinConstraint(HalfEdgeData triangleData, List<XVector2> constraints)
        {
            HashSet<HalfEdgeFace> trianglesToDelete = new HashSet<HalfEdgeFace>();

            //Store the triangles we flood fill in this queue
            Queue<HalfEdgeFace> trianglesToCheck = new Queue<HalfEdgeFace>();


            //Step 1. Find all half-edges in the current triangulation which are constraint
            //Maybe faster to find all constraintEdges for ALL constraints because we are doing this per hole and hull
            //We have to find ALL because some triangles are not connected and will thus be missed if we find just a single start-triangle
            //Is also needed when flood-filling so we dont jump over a constraint
            HashSet<HalfEdge> constraintEdges = FindAllConstraintEdges(constraints, triangleData);

            //Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
            //Each edge is associated with a face which should be deleted
            foreach (HalfEdge e in constraintEdges)
            {
                //Debug.LogWarning($"eee {e.GetHashCode()} {f(e.PrevEdge.Vertex.Position)} {f(e.Vertex.Position)}");
                if (!trianglesToCheck.Contains(e.Face))
                {
                    //DebugUtility.Print(e.Face, Test2.Navmesh.Normalizer);
                    trianglesToCheck.Enqueue(e.Face);
                }
            }

            //Debug.LogWarning($"constraintEdges -------- {constraintEdges.Count} {trianglesToCheck.Count}");
            //foreach (HalfEdge e in constraintEdges)
            //{
            //    DebugUtility.Print(e);
            //}
            //Debug.LogWarning("constraintEdges -------- after ");


            //Step 2. Find the rest of the triangles within the constraint by using a flood-fill algorithm
            List<HalfEdge> edgesToCheck = new List<HalfEdge>();

            int count = 0;
            while (true)
            {
                if (count++ > 1000)
                {
#if DEBUG_PATH
                        Recorder.Show(null);
#endif
                    throw new System.Exception("loop error");
                }

                //Stop if we are out of neighbors
                if (trianglesToCheck.Count == 0)
                {
                    break;
                }

                //Pick the first triangle in the list and investigate its neighbors
                HalfEdgeFace t = trianglesToCheck.Dequeue();
                //Debug.LogWarning("t-------------");
                //DebugUtility.Print(t, Test2.Navmesh.Normalizer);
                //Debug.LogWarning("t-------------");


                //Add it for deletion
                trianglesToDelete.Add(t);
                //Debug.LogWarning("add delete");

                //Investigate the triangles on the opposite sides of these edges
                edgesToCheck.Clear();

                edgesToCheck.Add(t.Edge);
                edgesToCheck.Add(t.Edge.NextEdge);
                edgesToCheck.Add(t.Edge.NextEdge.NextEdge);

                //A triangle is a neighbor within the constraint if:
                //- The neighbor is not an outer border meaning no neighbor exists
                //- If we have not already visited the neighbor
                //- If the edge between the neighbor and this triangle is not a constraint
                foreach (HalfEdge e in edgesToCheck)
                {
                    string nei = null;
                    if (e.OppositeEdge == null)
                        nei = "isnull";
                    else
                    {
                        //DebugUtility.Print(e.OppositeEdge.Face, Test2.Navmesh.Normalizer);
                        //nei = $"{f(e.OppositeEdge.PrevEdge.Vertex.Position)}  {f(e.OppositeEdge.Vertex.Position)}  hash{e.OppositeEdge.Face.GetHashCode()} ";
                    }
                    //Debug.LogWarning($" neighbor hash{e.Face.GetHashCode()} {f(e.PrevEdge.Vertex.Position)} {f(e.Vertex.Position)} {nei} ");
                    //No neighbor exists
                    if (e.OppositeEdge == null)
                    {
                        continue;
                    }

                    HalfEdgeFace neighbor = e.OppositeEdge.Face;

                    //Debug.LogWarning($"Contains {e.GetHashCode()} {trianglesToDelete.Contains(neighbor)} {trianglesToCheck.Contains(neighbor)} {constraintEdges.Contains(e)} ");

                    //We have already visited this neighbor
                    if (trianglesToDelete.Contains(neighbor) || trianglesToCheck.Contains(neighbor))
                    {
                        continue;
                    }

                    //This edge is a constraint and we can't jump across constraints 
                    if (constraintEdges.Contains(e)) // 所有边都在列表中，当另外一条边不在列表中时，将其林边加入并删除
                    {
                        continue;
                    }

                    trianglesToCheck.Enqueue(neighbor);
                }
            }

            //Debug.LogWarning("will delete --------");
            //foreach (HalfEdgeFace ff in trianglesToDelete)
            //{
            //    DebugUtility.Print(ff, Test2.Navmesh.Normalizer);
            //}
            //Debug.LogWarning("will delete -------- after ");

            return trianglesToDelete;
        }

        //Find all half-edges that are constraint
        private static HashSet<HalfEdge> FindAllConstraintEdges(List<XVector2> constraints, HalfEdgeData triangleData)
        {
            HashSet<HalfEdge> constrainEdges = new HashSet<HalfEdge>();


            //Create a new set with all constrains, and as we discover new constraints, we delete constrains, which will make searching faster
            //A constraint can only exist once!
            HashSet<Edge> constraintsEdges = new HashSet<Edge>();

            for (int i = 0; i < constraints.Count; i++)
            {
                XVector2 c_p1 = constraints[i];
                XVector2 c_p2 = constraints[XMath.ClampListIndex(i + 1, constraints.Count)];

                constraintsEdges.Add(new Edge(c_p1, c_p2));
            }


            //All edges we have to search
            HashSet<HalfEdge> edges = triangleData.Edges;

            foreach (HalfEdge e in edges)
            {
                //An edge is going TO a vertex
                XVector2 e_p1 = e.PrevEdge.Vertex.Position;
                XVector2 e_p2 = e.Vertex.Position;

                //Is this edge a constraint?
                foreach (Edge c_edge in constraintsEdges)
                {
                    if (e_p1.Equals(c_edge.P1) && e_p2.Equals(c_edge.P2))
                    {
                        constrainEdges.Add(e);

                        constraintsEdges.Remove(c_edge);

                        //Move on to the next edge
                        break;
                    }
                }

                //We have found all constraint, so don't need to search anymore
                if (constraintsEdges.Count == 0)
                {
                    break;
                }
            }

            return constrainEdges;
        }

        //
        // Try to restore the delaunay triangulation by flipping newly created edges
        //

        //This process is similar to when we created the original delaunay triangulation
        //This step can maybe be skipped if you just want a triangulation and Ive noticed its often not flipping any triangles
        private static void RestoreDelaunayTriangulation(XVector2 c_p1, XVector2 c_p2, List<HalfEdge> newEdges)
        {
            int count = 0;
            int flippedEdges = 0;
            //Repeat 4.1 - 4.3 until no further swaps take place
            while (true)
            {
                if (count++ > 1000)
                {
#if DEBUG_PATH
                        Recorder.Show(null);
#endif
                    throw new System.Exception("loop error");
                }

                bool hasFlippedEdge = false;

                //Step 4.1. Loop over each edge in the list of newly created edges
                for (int j = 0; j < newEdges.Count; j++)
                {
                    HalfEdge e = newEdges[j];

                    //Step 4.2. Let the newly created edge be defined by the vertices
                    XVector2 v_k = e.Vertex.Position;
                    XVector2 v_l = e.PrevEdge.Vertex.Position;

                    //If this edge is equal to the constrained edge, then skip to step 4.1
                    //because we are not allowed to flip the constrained edge
                    if ((v_k.Equals(c_p1) && v_l.Equals(c_p2)) || (v_l.Equals(c_p1) && v_k.Equals(c_p2)))
                    {
                        continue;
                    }

                    //Step 4.3. If the two triangles that share edge v_k and v_l don't satisfy the delaunay criterion,
                    //so that a vertex of one of the triangles is inside the circumcircle of the other triangle, flip the edge
                    //The third vertex of the triangle belonging to this edge
                    XVector2 v_third_pos = e.NextEdge.Vertex.Position;
                    //The vertice belonging to the triangle on the opposite side of the edge and this vertex is not a part of the edge
                    XVector2 v_opposite_pos = e.OppositeEdge.NextEdge.Vertex.Position;

                    //Test if we should flip this edge
                    if (HalfEdgeUtility.ShouldFlipEdge(v_l, v_k, v_third_pos, v_opposite_pos))
                    {
                        //Flip the edge
                        hasFlippedEdge = true;

                        HalfEdgeUtility.FlipTriangleEdge(e);

                        flippedEdges += 1;
                    }
                }

                //We have searched through all edges and havent found an edge to flip, so we cant improve anymore
                if (!hasFlippedEdge)
                {
                    //Debug.Log("Found a constrained delaunay triangulation in " + flippedEdges + " flips");

                    break;
                }
            }
        }

        private static bool IsEdgeInListOfEdges(HashSet<HalfEdge> edges, XVector2 p1, XVector2 p2)
        {
            foreach (HalfEdge e in edges)
            {
                //The vertices positions of the current triangle
                XVector2 e_p2 = e.Vertex.Position;
                XVector2 e_p1 = e.PrevEdge.Vertex.Position;
                //Check if edge has the same coordinates as the constrained edge
                //We have no idea about direction so we have to check both directions
                //This is fast because we only need to test one coordinate and if that 
                //coordinate doesn't match the edges can't be the same
                //We can't use a dictionary because we flip edges constantly so it would have to change?
                if (AreTwoEdgesTheSame(p1, p2, e_p1, e_p2))
                {
                    return true;
                }
            }

            return false;
        }

        //
        // Edge stuff
        //

        //Are two edges the same edge?
        private static bool AreTwoEdgesTheSame(XVector2 e1_p1, XVector2 e1_p2, XVector2 e2_p1, XVector2 e2_p2)
        {
            //Is e1_p1 part of this constraint?
            if ((e1_p1.Equals(e2_p1) || e1_p1.Equals(e2_p2)))
            {
                //Is e1_p2 part of this constraint?
                if ((e1_p2.Equals(e2_p1) || e1_p2.Equals(e2_p2)))
                {
                    return true;
                }
            }

            return false;
        }

        //
        // Find edges that intersect with a constraint
        //

        //Method 1. Brute force by testing all unique edges
        //Find all edges of the current triangulation that intersects with the constraint edge between p1 and p2
        private static Queue<HalfEdge> FindIntersectingEdges_BruteForce(HashSet<HalfEdge> edges, XVector2 c_p1, XVector2 c_p2)
        {
            //Should be in a queue because we will later plop the first in the queue and add edges in the back of the queue 
            Queue<HalfEdge> intersectingEdges = new Queue<HalfEdge>();

            //We also need to make sure that we are only adding unique edges to the queue
            //In the half-edge data structure we have an edge going in the opposite direction
            //and we only need to add an edge going in one direction
            HashSet<Edge> edgesInQueue = new HashSet<Edge>();

            //Loop through all edges and see if they are intersecting with the constrained edge
            foreach (HalfEdge e in edges)
            {
                //The position the edge is going to
                XVector2 e_p2 = e.Vertex.Position;
                //The position the edge is coming from
                XVector2 e_p1 = e.PrevEdge.Vertex.Position;

                //Has this edge been added, but in the opposite direction?
                bool find = false;
                foreach (Edge queueE in edgesInQueue)
                {
                    if (queueE.Equals(e_p1, e_p2))
                    {
                        find = true;
                        break;
                    }
                }
                if (find)
                {
                    continue;
                }

                //Is this edge intersecting with the constraint?
                if (IsEdgeCrossingEdge(e_p1, e_p2, c_p1, c_p2, true))  //这里需要高清度精确，因为使用低精度，会使靠近边时限制无效，从而导致错误
                {
                    //If so add it to the queue of edges
                    intersectingEdges.Enqueue(e);

                    edgesInQueue.Add(new Edge(e_p1, e_p2));
                }
            }

            return intersectingEdges;
        }

        //Is an edge crossing another edge? 
        private static bool IsEdgeCrossingEdge(XVector2 e1_p1, XVector2 e1_p2, XVector2 e2_p1, XVector2 e2_p2, bool highPrecision = false)
        {
            //We will here run into floating point precision issues so we have to be careful
            //To solve that you can first check the end points 
            //and modify the line-line intersection algorithm to include a small epsilon

            //First check if the edges are sharing a point, if so they are not crossing
            if (e1_p1.Equals(e2_p1) || e1_p1.Equals(e2_p2) || e1_p2.Equals(e2_p1) || e1_p2.Equals(e2_p2))
            {
                return false;
            }

            //Then check if the lines are intersecting
            if (!XMath.LineLine(new Edge(e1_p1, e1_p2), new Edge(e2_p1, e2_p2), includeEndPoints: false, highPrecision))
            {
                return false;
            }

            return true;
        }

        //
        // Remove the edges that intersects with a constraint by flipping triangles
        //

        //The idea here is that all possible triangulations for a set of points can be found 
        //by systematically swapping the diagonal in each convex quadrilateral formed by a pair of triangles
        //So we will test all possible arrangements and will always find a triangulation which includes the constrained edge
        private static List<HalfEdge> RemoveIntersectingEdges(XVector2 v_i, XVector2 v_j, Queue<HalfEdge> intersectingEdges)
        {
            List<HalfEdge> newEdges = new List<HalfEdge>();

            int count = 0;
            //While some edges still cross the constrained edge, do steps 3.1 and 3.2
            while (intersectingEdges.Count > 0)
            {
                if (count++ > 1000)
                {
#if DEBUG_PATH
                        Recorder.Show(null);
#endif
                    throw new System.Exception("loop error");
                }
                //Step 3.1. Remove an edge from the list of edges that intersects the constrained edge
                HalfEdge e = intersectingEdges.Dequeue();

                //The vertices belonging to the two triangles
                XVector2 v_k = e.Vertex.Position;
                XVector2 v_l = e.PrevEdge.Vertex.Position;
                XVector2 v_3rd = e.NextEdge.Vertex.Position;
                //The vertex belonging to the opposite triangle and isn't shared by the current edge
                XVector2 v_opposite_pos = e.OppositeEdge.NextEdge.Vertex.Position;

                //if (count > 950)
                //{
                //    Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
                //    Debug.LogWarning($"[constraint] remove intersect {f(v_i)} {f(v_j)} [[ {f(v_k)} {f(v_l)} {f(v_3rd)} ]] {f(v_opposite_pos)} {GeometryUtility.IsQuadrilateralConvex(v_k, v_l, v_3rd, v_opposite_pos)} ");
                //    Debug.LogWarning($"[constraint] in same line {EdgeSet.InSameLine(new Edge(v_3rd, v_l), new Edge(v_3rd, v_opposite_pos))}");
                //}

                // if in same line then continue
                if (EdgeSet.InSameLine(new Edge(v_3rd, v_l), new Edge(v_3rd, v_opposite_pos)))
                {
                    continue;
                }

                //Step 3.2. If the two triangles don't form a convex quadtrilateral
                //place the edge back on the list of intersecting edges (because this edge cant be flipped) 
                //and go to step 3.1
                if (!DelaunayUtility.IsQuadrilateralConvex(v_k, v_l, v_3rd, v_opposite_pos))
                {
                    //Debug.LogWarning("[constraint] no flip");
                    if (intersectingEdges.Count > 0)
                        intersectingEdges.Enqueue(e);

                    continue;
                }
                else
                {
                    //Debug.LogWarning("[constraint] flip");
                    //Flip the edge like we did when we created the delaunay triangulation
                    HalfEdgeUtility.FlipTriangleEdge(e);

                    //The new diagonal is defined by the vertices
                    XVector2 v_m = e.Vertex.Position;
                    XVector2 v_n = e.PrevEdge.Vertex.Position;

                    //If this new diagonal intersects with the constrained edge, add it to the list of intersecting edges
                    if (IsEdgeCrossingEdge(v_i, v_j, v_m, v_n))
                    {
                        intersectingEdges.Enqueue(e);
                    }
                    //Place it in the list of newly created edges
                    else
                    {
                        newEdges.Add(e);
                    }
                }
            }

            return newEdges;
        }
    }

}
