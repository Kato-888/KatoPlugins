using System;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Collections;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.DocObjects;
using Rhino.UI.Gumball;
using System.Runtime.InteropServices;


namespace KatoPlugins.Commands
{
    public class KatoClippingBox : Rhino.Commands.TransformCommand
    {
        public Box basebox;
        
        static DrawClippingPlanesConduit m_draw_clipping_planes;
        static DisplayClippingBoxConduit m_display_clipping_box;
        


        static KatoClippingBox _instance;
        public KatoClippingBox()
        {
            _instance = this;
        }

        ///<summary>The only instance of the XXtest command.</summary>
        public static KatoClippingBox Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "KatoClippingBox"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            if (basebox.IsValid == false)
            {
                Rhino.Input.RhinoGet.GetBox(out basebox);
            }

            BoundingBox bbox;
            bbox = basebox.BoundingBox;
            var newboxcorners = bbox.GetCorners();

            if (newboxcorners == null)
                return Result.Failure;

            

            //make plane for box)
            Plane plane = new Plane(newboxcorners[0], newboxcorners[1], newboxcorners[3]);

            //gumball settings
            var X_plus_go = new GumballObject();
            var X_minus_go = new GumballObject();
            var Y_plus_go = new GumballObject();
            var Y_minus_go = new GumballObject();
            var Z_plus_go = new GumballObject();
            var Z_minus_go = new GumballObject();

            var X_plus_dc = new GumballDisplayConduit();
            var X_minus_dc = new GumballDisplayConduit();
            var Y_plus_dc = new GumballDisplayConduit();
            var Y_minus_dc = new GumballDisplayConduit();
            var Z_plus_dc = new GumballDisplayConduit();
            var Z_minus_dc = new GumballDisplayConduit();

            var X_plus_gas = XPlusGumballAppearanceSettings();
            var X_minus_gas = XMinusGumballAppearanceSettings();
            var Y_plus_gas = YPlusGumballAppearanceSettings();
            var Y_minus_gas = YMinusGumballAppearanceSettings();
            var Z_plus_gas = ZPlusGumballAppearanceSettings();
            var Z_minus_gas = ZMinusGumballAppearanceSettings();


            //This is flexible box 
            ClipGumballBox New_box;

            if (m_draw_clipping_planes == null)
            {
                New_box = new ClipGumballBox(plane, newboxcorners);
                m_draw_clipping_planes = new DrawClippingPlanesConduit(New_box);
                m_draw_clipping_planes.Enabled = true;
                doc.Views.Redraw();
            }
            else
            {
                m_draw_clipping_planes.Enabled = false;
                m_draw_clipping_planes = null;
                m_display_clipping_box.Enabled = false;
                m_display_clipping_box = null;

                New_box = new ClipGumballBox(plane, newboxcorners);
                m_draw_clipping_planes = new DrawClippingPlanesConduit(New_box);
                m_draw_clipping_planes.Enabled = true;
                doc.Views.Redraw();
            }

            if (New_box == null)
                return Result.Failure;

            while (true)
            {
                X_plus_go.SetFromPlane(New_box.X_plus_plane); //set point of gumball
                X_minus_go.SetFromPlane(New_box.X_minus_plane);
                Y_plus_go.SetFromPlane(New_box.Y_plus_plane);
                Y_minus_go.SetFromPlane(New_box.Y_minus_plane);
                Z_plus_go.SetFromPlane(New_box.Z_plus_plane);
                Z_minus_go.SetFromPlane(New_box.Z_minus_plane);

                X_plus_dc.SetBaseGumball(X_plus_go, X_plus_gas);
                X_minus_dc.SetBaseGumball(X_minus_go, X_minus_gas);
                Y_plus_dc.SetBaseGumball(Y_plus_go, Y_plus_gas);
                Y_minus_dc.SetBaseGumball(Y_minus_go, Y_minus_gas);
                Z_plus_dc.SetBaseGumball(Z_plus_go, Z_plus_gas);
                Z_minus_dc.SetBaseGumball(Z_minus_go, Z_minus_gas);

                X_plus_dc.Enabled = true;
                X_minus_dc.Enabled = true;
                Y_plus_dc.Enabled = true;
                Y_minus_dc.Enabled = true;
                Z_plus_dc.Enabled = true;
                Z_minus_dc.Enabled = true;

                var gx = new ClipGumballBoxGetPoint(New_box, doc,/* viewPortID,*/ m_draw_clipping_planes, X_plus_dc, X_minus_dc, Y_plus_dc, Y_minus_dc, Z_plus_dc, Z_minus_dc/*, rv, ref removed*/);

                gx.SetCommandPrompt("Drag gumball. Press Enter when done");
               
                gx.AcceptNothing(true); //this line is for "press Enter key"
                gx.MoveGumball();

                X_plus_dc.Enabled = false; //To enable gumball arrow
                X_minus_dc.Enabled = false;
                Y_plus_dc.Enabled = false;
                Y_minus_dc.Enabled = false;
                Z_plus_dc.Enabled = false;
                Z_minus_dc.Enabled = false;

                if (gx.CommandResult() != Result.Success)
                    break;

                var res = gx.Result();
                if (res == GetResult.Point)
                {
                    var new_points = New_box.CornerPoints;
                    New_box = new ClipGumballBox(plane, new_points);

                    m_draw_clipping_planes.Enabled = false;
                    m_draw_clipping_planes = gx.m_plain;
                    m_draw_clipping_planes.Enabled = true;
                    if (doc.Views.RedrawEnabled == true)
                        doc.Views.Redraw();
                    continue;
                }

                else if (res == GetResult.Nothing) //When press Enter key
                {

                    newboxcorners = New_box.CornerPoints;

                    var new_box = new Box(plane, newboxcorners);

                    New_box.SetConstructionPlane(doc, "Top"); //To set construction plane for "Top", "Front", "Right"  not other views so far
                    New_box.SetConstructionPlane(doc, "Right");
                    New_box.SetConstructionPlane(doc, "Front");

                    m_draw_clipping_planes.Enabled = false;
                    m_draw_clipping_planes = gx.m_plain;
                    m_draw_clipping_planes.Enabled = true;
                    doc.Views.Redraw();

                    basebox = new_box;
                   
                    if (basebox.IsValid)
                    {

                        m_display_clipping_box = new DisplayClippingBoxConduit(basebox);
                        
                        m_display_clipping_box.Enabled = true;
                    }

                    
                }

                break;
            }

            doc.Views.Redraw();

            return Result.Success;
        }



        private GumballAppearanceSettings XPlusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }

        private GumballAppearanceSettings XMinusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }
        private GumballAppearanceSettings YPlusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }

        private GumballAppearanceSettings YMinusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }

        private GumballAppearanceSettings ZPlusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }

        private GumballAppearanceSettings ZMinusGumballAppearanceSettings()
        {
            var gas = new GumballAppearanceSettings
            {
                RelocateEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                TranslateXEnabled = false,
                TranslateXYEnabled = false,
                TranslateYEnabled = false,
                TranslateYZEnabled = false,
                TranslateZEnabled = true,
                TranslateZXEnabled = false,
                MenuEnabled = false
            };
            return gas;
        }
    }
}