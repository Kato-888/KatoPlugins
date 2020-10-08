using System;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.UI.Gumball;



namespace KatoPlugins
{

    class DrawClippingPlanesConduit : DisplayConduit
    {
        private readonly ClipGumballBox d_box;
        private readonly Point3d[] d_corners;
        private readonly Point3d d_center;

        private readonly Point3d XplusCenter;
        private readonly Vector3d XplusVec;
        private readonly Point3d XminusCenter;
        private readonly Vector3d XminusVec;

        private readonly Point3d YplusCenter;
        private readonly Vector3d YplusVec;
        private readonly Point3d YminusCenter;
        private readonly Vector3d YminusVec;

        private readonly Point3d ZplusCenter;
        private readonly Vector3d ZplusVec;
        private readonly Point3d ZminusCenter;
        private readonly Vector3d ZminusVec;


        public DrawClippingPlanesConduit(ClipGumballBox box)
        {
            d_box = box;


            d_corners = d_box.box.GetCorners();
            d_center = d_box.Center;

            Plane planeXplus = new Plane(d_corners[2], d_corners[1], d_corners[6]);
            XplusCenter = planeXplus.ClosestPoint(d_center);
            XplusVec = new Vector3d(d_center - XplusCenter);

            Plane planeXminus = new Plane(d_corners[0], d_corners[3], d_corners[4]);
            XminusCenter = planeXminus.ClosestPoint(d_center);
            XminusVec = new Vector3d(d_center - XminusCenter);

            Plane planeYplus = new Plane(d_corners[3], d_corners[2], d_corners[7]);
            YplusCenter = planeYplus.ClosestPoint(d_center);
            YplusVec = new Vector3d(d_center - YplusCenter);


            Plane planeYminus = new Plane(d_corners[1], d_corners[0], d_corners[5]);
            YminusCenter = planeYminus.ClosestPoint(d_center);
            YminusVec = new Vector3d(d_center - YminusCenter);


            Plane planeZplus = new Plane(d_corners[5], d_corners[4], d_corners[6]);
            ZplusCenter = planeZplus.ClosestPoint(d_center);
            ZplusVec = new Vector3d(d_center - ZplusCenter);


            Plane planeZminus = new Plane(d_corners[0], d_corners[1], d_corners[3]);
            ZminusCenter = planeZminus.ClosestPoint(d_center);
            ZminusVec = new Vector3d(d_center - ZminusCenter);


        }

        // this is called every frame inside the drawing code so try to do as little as possible
        // in order to not degrade display speed. Don't create new objects if you don't have to as this
        // will incur an overhead on the heap and garbage collection.
        protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
        {
            if (d_box != null)
            {
                BoundingBox m_bbox = d_box.box.BoundingBox;
                base.CalculateBoundingBox(e);
                // Since we are dynamically drawing geometry, we needed to override
                // CalculateBoundingBox. Otherwise, there is a good chance that our
                // dynamically drawing geometry would get clipped.

                // Union the mesh's bbox with the scene's bounding box
                e.IncludeBoundingBox(m_bbox);

            }
        }


        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            e.Display.AddClippingPlane(XplusCenter, XplusVec);
            e.Display.AddClippingPlane(XminusCenter, XminusVec);
            e.Display.AddClippingPlane(YplusCenter, YplusVec);
            e.Display.AddClippingPlane(YminusCenter, YminusVec);
            e.Display.AddClippingPlane(ZplusCenter, ZplusVec);
            e.Display.AddClippingPlane(ZminusCenter, ZminusVec);


        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

        }

    }

    /// <summary>
    /// Draw dotted line boundingbox when the command finished
    /// </summary>
    public class DisplayClippingBoxConduit : DisplayConduit
    {
        private readonly Box d_box;
        private readonly Line[] edge_lines;


        public DisplayClippingBoxConduit(Box box)
        {
            d_box = box;
            edge_lines = d_box.BoundingBox.GetEdges();

        }

        protected override void DrawForeground(Rhino.Display.DrawEventArgs e)
        {
            foreach (Line edge in edge_lines)
            {
                e.Display.DrawDottedLine(edge, System.Drawing.Color.Black);
            }

        }
    }
}
