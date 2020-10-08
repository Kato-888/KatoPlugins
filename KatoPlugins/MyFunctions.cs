using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects.Tables;
using Rhino.DocObjects;
using Rhino.Display;


namespace KatoPlugins
{
    class MyFunctions
    {

        /// <summary>
        /// To change and adjust the size of cylinder
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="cylinder"></param>
        /// <param name="new_radius"></param>
        /// <param name="diameter"></param>
        /// <param name="obj"></param>
        /// <param name="NewCylinder"></param>
        /// <returns></returns>
        public static Cylinder ChangeCylinderBore(RhinoDoc doc, Cylinder cylinder, double new_radius, bool diameter, RhinoObject obj, Cylinder NewCylinder)
        {

            if (diameter == true)
                new_radius /= 2;
            var circle = cylinder.CircleAt(0.0);
            var plane = circle.Plane;
            var origin = plane.Origin;
            var radius = cylinder.Radius;



            // Calculate a plane-aligned bounding box.
            // Calculating the bounding box from the runtime object, instead
            // of a copy of the geometry, will produce a more accurate result.
            var world_to_plane = Transform.ChangeBasis(Plane.WorldXY, plane);
            var bbox = obj.Geometry.GetBoundingBox(world_to_plane);

            // Move the cylinder's plane to the base of the bounding box.
            // Create a plane through the base of the bounding box.
            var bbox_plane = new Plane(
                bbox.Corner(true, true, true),
                bbox.Corner(false, true, true),
                bbox.Corner(true, false, true)
                );
            // Transform the plane to the world xy-plane
            var plane_to_world = Transform.ChangeBasis(plane, Plane.WorldXY);
            bbox_plane.Transform(plane_to_world);
            // Project the cylinder plane's origin onto the bounding box plane
            plane.Origin = bbox_plane.ClosestPoint(origin);

            // Cylinder height is bounding box height
            var pt0 = bbox.Corner(true, true, true);
            var pt1 = bbox.Corner(true, true, false);
            var height = pt0.DistanceTo(pt1);


            // Create a new cylinder
            var newradius = new_radius; // "new_radius" is from parameter
            var new_circle = new Circle(plane, newradius);
            var arccurve = new ArcCurve(new_circle);

            NewCylinder = new Cylinder(new_circle, height);
            //var rev_surface = new_cylinder.ToRevSurface();
            //doc.Objects.AddSurface(rev_surface);


            //Extrusion Cyl = Extrusion.Create(arccurve, height, true);

            //doc.Objects.AddExtrusion(Cyl);

            //doc.Objects.Delete(obj);
            //go.Dispose();
            //doc.Views.Redraw();
            return NewCylinder;



        }

        public static void DisplayPipeline_draw(Line line, object sender, Rhino.Display.DrawEventArgs e)
        {
            e.Display.DrawDottedLine(line, System.Drawing.Color.RosyBrown);
        }






        /// <summary>
        /// Purge current ClipPlanes for to set new planes
        /// </summary>
        /// <param name="rv"></param>
        /// <param name="removed"></param>
        public static void PurgeClipPlane(RhinoViewport rv, ref int removed)
        {
            ClippingPlaneObject[] clipObjects = RhinoDoc.ActiveDoc.Objects.FindClippingPlanesForViewport(rv);
            if (clipObjects.Length < 1 || clipObjects == null) return;
            removed = clipObjects.Length;
            for (int i = 0; i < clipObjects.Length; i++)
            {
                ClippingPlaneObject cpo = clipObjects[i];
                RhinoDoc.ActiveDoc.Objects.Purge(cpo);
            }
        }




        /// <summary>
        /// Set or create a new layer 
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Rhino.DocObjects.Layer SetLayer(RhinoDoc doc, string layername, System.Drawing.Color color)
        {
            if (string.IsNullOrEmpty(layername) || !Rhino.DocObjects.Layer.IsValidName(layername))
                return null;

            //dose the layer already exist?
            Rhino.DocObjects.Layer layer = doc.Layers.FindName(layername);

            if (layer != null)
            {
                //the layer already exist
                if (layer.Index >= 0)
                    doc.Layers.SetCurrentLayerIndex(layer.Index, false);

            }
            else
            {
                layer = new Rhino.DocObjects.Layer();

                layer.Name = layername;
                layer.Color = color;

                //we have to create a new layer
                int layer_index = doc.Layers.Add(layer);
                if (layer_index >= 0)
                    doc.Layers.SetCurrentLayerIndex(layer_index, false);

            }


            return doc.Layers[layer.Index];
        }

        public static Rhino.DocObjects.Layer SetLayer(RhinoDoc doc, string layername, System.Drawing.Color color, Rhino.DocObjects.Layer parentlayer)
        {
            if (string.IsNullOrEmpty(layername) || !Rhino.DocObjects.Layer.IsValidName(layername))
                return null;

            //dose the layer already exist?
            Rhino.DocObjects.Layer layer = doc.Layers.FindName(layername);

            if (layer != null)
            {
                //the layer already exist
                if (layer.Index >= 0)
                    doc.Layers.SetCurrentLayerIndex(layer.Index, false);

            }
            else
            {
                layer = new Rhino.DocObjects.Layer();

                if (parentlayer.Id != null)
                    layer.ParentLayerId = parentlayer.Id;

                layer.Name = layername;
                layer.Color = color;

                //we have to create a new layer
                int layer_index = doc.Layers.Add(layer);
                if (layer_index >= 0)
                    doc.Layers.SetCurrentLayerIndex(layer_index, false);

            }


            return doc.Layers[layer.Index];
        }

    }


}
