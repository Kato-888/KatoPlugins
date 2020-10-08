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

    /// <summary>
    /// ClipGumballBox
    /// </summary>
    class ClipGumballBox
    {
        readonly Vector3d vec_x;
        readonly Vector3d vec_y;
        readonly Vector3d vec_z;
        readonly Point3d[] m_corners;
        Box m_box;
        Plane m_plane;
        Brep m_brep;

        public ClipGumballBox(Plane plane, Point3d[] corners)
        {
            vec_x = new Vector3d(corners[1] - corners[0]);
            vec_y = new Vector3d(corners[3] - corners[0]);
            vec_z = new Vector3d(corners[4] - corners[0]);

            if (Create(plane, corners))
            {
                m_plane = plane;
                m_corners = corners;
                vec_x = corners[1] - corners[0];
                vec_y = corners[3] - corners[0];
                vec_z = corners[4] - corners[0];
            }

        }

        public bool Create(Plane plane, Point3d[] cornerpoints)
        {
            double distanceX = cornerpoints[0].DistanceTo(cornerpoints[1]);
            double distanceY = cornerpoints[0].DistanceTo(cornerpoints[3]);
            double distanceZ = cornerpoints[0].DistanceTo(cornerpoints[4]);
            var rc = false;
            if (distanceX > 0.0 && distanceY > 0.0 && distanceZ > 0.0)
            {
                m_box = new Box(plane, cornerpoints);

                rc = m_box.IsValid;
                if (rc)
                {
                    m_brep = ToBrep;
                    rc = m_brep.IsValid;
                }
            }
            return rc;
        }

        public Box box
        {
            get
            {
                return m_box;
            }
        }


        public Point3d Center
        {
            get
            {
                return m_box.Center;
            }
        }

        public Vector3d Xvector
        {
            get
            {
                return vec_x;
            }
        }
        public Vector3d Yvector
        {
            get
            {
                return vec_y;
            }
        }


        public Vector3d Zvector
        {
            get
            {
                return vec_z;
            }
        }

        public Point3d[] CornerPoints
        {
            get
            {
                return m_box.GetCorners();

            }
            set
            {
                Create(m_plane, value);
            }
        }

        public Point3d[] BaseCornerPoints
        {
            get
            {
                return m_corners;
            }
        }

        public Plane CCPlane
        {
            get
            {
                return m_plane;
            }
        }

        public Plane X_plus_plane
        {
            get
            {
                var center = m_box.Center + (vec_x / 2);
                var y_axis = vec_y;
                y_axis.Unitize();
                var z_axis = vec_z;
                z_axis.Unitize();
                return new Plane(center, y_axis, z_axis);

            }
        }
        public Plane X_minus_plane
        {
            get
            {
                var center = m_box.Center + (vec_x / 2) * (-1);
                var y_axis = vec_y;
                y_axis.Unitize();
                var z_axis = vec_z;
                z_axis.Unitize();
                var plane = new Plane(center, y_axis, z_axis);
                plane.Flip();
                return plane;

            }
        }

        public Plane Y_plus_plane
        {
            get
            {
                var center = m_box.Center + (vec_y / 2);
                var x_axis = vec_x;
                x_axis.Unitize();
                var z_axis = vec_z;
                z_axis.Unitize();
                var plane = new Plane(center, x_axis, z_axis);
                plane.Flip();
                return plane;
            }
        }
        public Plane Y_minus_plane
        {
            get
            {
                var center = m_box.Center + (vec_y / 2) * (-1);
                var x_axis = vec_x;
                x_axis.Unitize();
                var z_axis = vec_z;
                z_axis.Unitize();
                return new Plane(center, x_axis, z_axis);

            }
        }

        public Plane Z_plus_plane
        {
            get
            {
                var center = m_box.Center + (vec_z / 2);
                var x_axis = vec_x;
                x_axis.Unitize();
                var y_axis = vec_y;
                y_axis.Unitize();
                return new Plane(center, x_axis, y_axis);
            }
        }
        public Plane Z_minus_plane
        {
            get
            {
                var center = m_box.Center + (vec_z / 2) * -1;
                var x_axis = vec_x;
                x_axis.Unitize();
                var y_axis = vec_y;
                y_axis.Unitize();
                var plane = new Plane(center, x_axis, y_axis);
                plane.Flip();
                return plane;

            }
        }

        public void Draw(DisplayPipeline display)
        {

            if (null != m_brep)
                display.DrawBox(m_box, Color.BlueViolet);
        }

        public Brep ToBrep
        {
            get
            {
                if (m_box.IsValid)
                    return m_box.ToBrep();
                return null;
            }
        }

        public void SetConstructionPlane(RhinoDoc doc, string viewname)
        {
            if (viewname == "Top")
            {
                var A_plane = Z_minus_plane;
                setCplane("Top", A_plane);
            }
            else if (viewname == "Front")
            {
                var A_plane = Y_minus_plane;
                setCplane("Front", A_plane);
            }
            else if (viewname == "Right")
            {
                var A_plane = X_plus_plane;
                setCplane("Right", A_plane);
            }

            void setCplane(string Viewname, Plane a_plane)
            {
                //To set Constructionplane for "Front" view
                Rhino.Display.RhinoView view = doc.Views.Find(Viewname, true);
                Rhino.DocObjects.ConstructionPlane cplane = view.ActiveViewport.GetConstructionPlane();
                Point3d origin = cplane.Plane.Origin;
                Plane pl = cplane.Plane;
                pl.Origin = a_plane.Origin;
                cplane.Plane = pl;
                view.ActiveViewport.SetConstructionPlane(cplane);
            }

        }

    }

    /// <summary>
    /// ClipGumballBoxGetPoint
    /// </summary>
    class ClipGumballBoxGetPoint : GetPoint
    {
        private ClipGumballBox m_Box;
        private readonly GumballDisplayConduit x_height_plus_dc;
        private readonly GumballDisplayConduit x_height_minus_dc;
        private readonly GumballDisplayConduit y_height_plus_dc;
        private readonly GumballDisplayConduit y_height_minus_dc;
        private readonly GumballDisplayConduit z_height_plus_dc;
        private readonly GumballDisplayConduit z_height_minus_dc;
        private DrawClippingPlanesConduit m_clipping_plane;

        private Point3d m_base_origin;
        private Point3d m_base_point;      
        public RhinoDoc m_doc;
        

        public ClipGumballBoxGetPoint(ClipGumballBox Box, RhinoDoc doc, DrawClippingPlanesConduit m_draw_clipping_plane,
            GumballDisplayConduit X_plus, GumballDisplayConduit X_minus,
            GumballDisplayConduit Y_plus, GumballDisplayConduit Y_minus,
            GumballDisplayConduit Z_plus, GumballDisplayConduit Z_minus)
        {
            m_Box = Box;
            x_height_plus_dc = X_plus;
            x_height_minus_dc = X_minus;
            y_height_plus_dc = Y_plus;
            y_height_minus_dc = Y_minus;
            z_height_plus_dc = Z_plus;
            z_height_minus_dc = Z_minus;


            m_clipping_plane = m_draw_clipping_plane;

            m_base_origin = Point3d.Unset;
            m_base_point = Point3d.Unset;

            m_doc = doc;

        }

        public DrawClippingPlanesConduit m_plain
        {
            get
            {
                return m_clipping_plane;
            }
        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            if (x_height_plus_dc.PickResult.Mode != GumballMode.None || x_height_minus_dc.PickResult.Mode != GumballMode.None ||
                y_height_plus_dc.PickResult.Mode != GumballMode.None || y_height_minus_dc.PickResult.Mode != GumballMode.None ||
                z_height_plus_dc.PickResult.Mode != GumballMode.None || z_height_minus_dc.PickResult.Mode != GumballMode.None)
                return;

            m_base_origin = Point3d.Unset;
            m_base_point = Point3d.Unset;

            x_height_plus_dc.PickResult.SetToDefault();
            x_height_minus_dc.PickResult.SetToDefault();
            y_height_plus_dc.PickResult.SetToDefault();
            y_height_minus_dc.PickResult.SetToDefault();
            z_height_plus_dc.PickResult.SetToDefault();
            z_height_minus_dc.PickResult.SetToDefault();

            var pick_context = new PickContext
            {
                View = e.Viewport.ParentView,
                PickStyle = PickStyle.PointPick
            };

            var xform = e.Viewport.GetPickTransform(e.WindowPoint);
            pick_context.SetPickTransform(xform);

            Line pick_line;
            e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out pick_line);

            pick_context.PickLine = pick_line;

            // try picking one of the gumballs
            if (x_height_plus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.X_plus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }

            else if (x_height_minus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.X_minus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }

            else if (y_height_plus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.Y_plus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }

            else if (y_height_minus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.Y_minus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }
            else if (z_height_plus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.Z_plus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }

            else if (z_height_minus_dc.PickGumball(pick_context, this))
            {
                m_base_origin = m_Box.Z_minus_plane.ClosestPoint(m_Box.Center);
                m_base_point = e.Point;
            }

        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            if (m_base_origin.IsValid && m_base_point.IsValid)
            {
                Line world_line;
                if (e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out world_line))
                {
                    var dir = e.Point - m_base_point;

                    var len = dir.Length;
                    if (m_base_origin.DistanceTo(e.Point) < m_base_origin.DistanceTo(m_base_point))
                        len = -len;

                    if (x_height_plus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        x_height_plus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var xvec = m_Box.Xvector;
                        xvec.Unitize();
                        xvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0];
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1] + xvec;
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2] + xvec;
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3];
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4];
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5] + xvec;
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6] + xvec;
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7];

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }

                    else if (x_height_minus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        x_height_minus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var reverseXvec = m_Box.Xvector * (-1);
                        reverseXvec.Unitize();
                        reverseXvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0] + reverseXvec;
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1];
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2];
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3] + reverseXvec;
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4] + reverseXvec;
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5];
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6];
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7] + reverseXvec;

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }

                    else if (y_height_plus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        y_height_plus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var yvec = m_Box.Yvector;
                        yvec.Unitize();
                        yvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0];
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1];
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2] + yvec;
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3] + yvec;
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4];
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5];
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6] + yvec;
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7] + yvec;

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }

                    else if (y_height_minus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        y_height_minus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var reverseYvec = m_Box.Yvector * (-1);
                        reverseYvec.Unitize();
                        reverseYvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0] + reverseYvec;
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1] + reverseYvec;
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2];
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3];
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4] + reverseYvec;
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5] + reverseYvec;
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6];
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7];

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }

                    else if (z_height_plus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        z_height_plus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var zvec = m_Box.Zvector;
                        zvec.Unitize();
                        zvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0];
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1];
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2];
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3];
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4] + zvec;
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5] + zvec;
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6] + zvec;
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7] + zvec;

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }

                    else if (z_height_minus_dc.PickResult.Mode != GumballMode.None)
                    {
                        // update height_plus gumball
                        z_height_minus_dc.UpdateGumball(e.Point, world_line);

                        // update Box
                        Point3d[] NewCornerPoints = new Point3d[8];

                        var reverseZvec = m_Box.Zvector * (-1);
                        reverseZvec.Unitize();
                        reverseZvec *= len;

                        NewCornerPoints[0] = m_Box.BaseCornerPoints[0] + reverseZvec;
                        NewCornerPoints[1] = m_Box.BaseCornerPoints[1] + reverseZvec;
                        NewCornerPoints[2] = m_Box.BaseCornerPoints[2] + reverseZvec;
                        NewCornerPoints[3] = m_Box.BaseCornerPoints[3] + reverseZvec;
                        NewCornerPoints[4] = m_Box.BaseCornerPoints[4];
                        NewCornerPoints[5] = m_Box.BaseCornerPoints[5];
                        NewCornerPoints[6] = m_Box.BaseCornerPoints[6];
                        NewCornerPoints[7] = m_Box.BaseCornerPoints[7];

                        m_Box.CornerPoints = NewCornerPoints;

                        m_clipping_plane.Enabled = false;
                        m_clipping_plane = new DrawClippingPlanesConduit(m_Box);
                        m_clipping_plane.Enabled = true;
                        m_doc.Views.Redraw();
                    }


                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {
            m_Box.Draw(e.Display);

            // Disable default GetPoint drawing by not calling the base class
            // implementation. All aspects of gumball display are handled by 
            // GumballDisplayConduit
            
        }

        public GetResult MoveGumball()
        {
            var rc = Get(true);
            return rc;
        }

    }




}

