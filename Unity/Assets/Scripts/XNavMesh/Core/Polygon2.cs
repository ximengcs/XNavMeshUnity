using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XFrame.PathFinding;

namespace XFrame.PathFinding
{
    //Polygon in 2d space
    public struct Polygon
    {
        public List<XVector2> vertices;


        public Polygon(List<XVector2> vertices)
        {
            this.vertices = vertices;
        }
    }
}
