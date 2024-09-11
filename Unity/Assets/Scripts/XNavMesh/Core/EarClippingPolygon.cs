
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    //Data structure to make it easier to work with holes
    public class EarClippingPolygon
    {
        public Polygon polygon;

        //the vertex in the list with the maximum x-value
        public XVector2 maxX_Vert;

        //The position in the list where the maxX vert is 
        public int maxX_ListPos;

        //ID number, which will make debugging easier
        public int id = -1;

        public List<XVector2> Vertices { get { return polygon.vertices; } }


        public EarClippingPolygon(Polygon polygon)
        {
            this.polygon = polygon;

            CalculateMaxXValue();
        }


        //Find the vertex with the maximum x-value
        private void CalculateMaxXValue()
        {
            List<XVector2> vertices = polygon.vertices;

            this.maxX_Vert = vertices[0];

            this.maxX_ListPos = 0;

            for (int i = 1; i < vertices.Count; i++)
            {
                XVector2 v = vertices[i];

                if (v.X > maxX_Vert.X)
                {
                    this.maxX_Vert = v;

                    this.maxX_ListPos = i;
                }
            }
        }


        //Find which position in the list a vertex has
        //If there are multiple, we want the last one
        public int GetLastListPos(XVector2 pos)
        {
            List<XVector2> vertices = polygon.vertices;

            int listPos = -1;

            for (int i = 0; i < vertices.Count; i++)
            {
                if (pos.Equals(vertices[i]))
                {
                    listPos = i;

                    //In some cases there are multiple of this vertices and we want the last one
                    //So we want stop searching after finding the first one
                    //break;
                }
            }

            return listPos;
        }
    }
}
