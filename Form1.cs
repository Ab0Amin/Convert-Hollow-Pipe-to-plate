using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tekla.Structures.Model;
using T3D = Tekla.Structures.Geometry3d;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model.UI;
using Tekla.Structures.Solid;


namespace Convert_Hollow_Pipe_to_plate
{
    public partial class Form1 : Form
    {
        Model myModel = new Model();
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Picker input = new Picker();
            Part pipe = input.PickObject(Picker.PickObjectEnum.PICK_ONE_PART) as Part;
            CoordinateSystem co = pipe.GetCoordinateSystem();
            ArrayList centerPoints = pipe.GetCenterLine(true);
            Vector vecy = new Vector((centerPoints[1] as T3D.Point)-(centerPoints[0]as T3D.Point));
            vecy.Normalize();
            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane((centerPoints[0] as T3D.Point), vecy, co.AxisY));
            double length = 200,thickness = 10 , diam = 1000;
            pipe.GetReportProperty("PROFILE.PLATE_THICKNESS",ref thickness);
            pipe.GetReportProperty("HEIGHT", ref diam);
            pipe.GetReportProperty("LENGTH_NET", ref length);
          Solid solid =   pipe.GetSolid(Solid.SolidCreationTypeEnum.NORMAL );
          T3D.Point min = solid.MinimumPoint;
          T3D.Point zero = new T3D.Point(min.X,0,0);

          PolyBeam plate=  polyPlate(pipe, length, thickness, diam,zero);
       ModelObjectEnumerator mo=   pipe.GetBooleans();
       while (mo.MoveNext())
       {
           CutPlane cut = mo.Current as CutPlane;
           if (cut !=null)
           {
               CutPlane plateCut = new CutPlane();
               plateCut.Father = plate;
               plateCut.Plane = cut.Plane;
               plateCut.Insert();
           }

       }

            groovebetween2points( plate,  thickness,  zero, zero + length*vecy, T3D.Vector normalvec, T3D.Vector insidevec, T3D.Point stiffmidpoint, double wsize, double wsize2, int wtype, int wtype2, double wangle, double wangle2, double extension)
    
            myModel.CommitChanges();



        }

        private static PolyBeam polyPlate(Part pipe, double length, double thickness, double diam, T3D.Point zeroPoint)
        {
            PolyBeam plate = new PolyBeam();
            plate.Profile.ProfileString = "PL" + length + "*" + thickness;
            plate.Material = pipe.Material;
            plate.AssemblyNumber = pipe.AssemblyNumber;
            plate.PartNumber = pipe.PartNumber;
            plate.Position.Depth = Position.DepthEnum.BEHIND;
            plate.Position.Rotation = Position.RotationEnum.FRONT;
            plate.Position.Plane = Position.PlaneEnum.LEFT;
            ArrayList contour_points = new ArrayList();
            Vector Z = new Vector(0, 0, 1);
            Vector Y = new Vector(0, 1, 0);
            T3D.Point p1 = zeroPoint + diam / 2 * Y;
            T3D.Point p2 = zeroPoint + diam / 2 * Z;
            T3D.Point p3 = zeroPoint - diam / 2 * Y;
            T3D.Point p4 = zeroPoint - diam / 2 * Z;
            T3D.Point p5 = zeroPoint + diam / 2 * Y;
            Chamfer c = new Chamfer();
            c.Type = Chamfer.ChamferTypeEnum.CHAMFER_ARC_POINT;
            ContourPoint cp1 = new ContourPoint(p1, c);
            ContourPoint cp2 = new ContourPoint(p2, c);
            ContourPoint cp3 = new ContourPoint(p3, c);
            ContourPoint cp4 = new ContourPoint(p4, c);
            ContourPoint cp5 = new ContourPoint(p5, c);
            contour_points.Add(cp1);
            contour_points.Add(cp2);
            contour_points.Add(cp3);
            contour_points.Add(cp4);
            contour_points.Add(cp5);
            plate.Contour.ContourPoints = contour_points;
            plate.Insert();
            return plate;
        }

        private void groovebetween2points(Part cutpart, double cutthk, T3D.Point p1, T3D.Point p2, T3D.Vector normalvec, T3D.Vector insidevec, T3D.Point stiffmidpoint, double wsize, double wsize2, int wtype, int wtype2, double wangle, double wangle2, double extension)
        {
            normalvec.Normalize();
            insidevec.Normalize();
            T3D.GeometricPlane faceplane = new T3D.GeometricPlane(p1, new T3D.Vector(p2 - p1), normalvec);
            // T3D.Vector insidevec = new T3D.Vector(stiffmidpoint - T3D.Projection.PointToPlane(stiffmidpoint, faceplane));
            insidevec.Normalize();

            T3D.Point mymidpoint = getmidpoint(p1, p2);

            T3D.Point gr1p1 = mymidpoint + cutthk / 2 * normalvec;
            T3D.Point gr1p2 = gr1p1 - wsize * normalvec;
            T3D.Point gr1p3 = gr1p1 + wsize * Math.Tan(wangle * Math.PI / 180) * insidevec;

            T3D.Point gr2p1 = mymidpoint - cutthk / 2 * normalvec;
            T3D.Point gr2p2 = gr2p1 + wsize2 * normalvec;
            T3D.Point gr2p3 = gr2p1 + wsize2 * Math.Tan(wangle2 * Math.PI / 180) * insidevec;

            string grooveprofie = "PL" + (T3D.Distance.PointToPoint(p1, p2) + 2 * extension);
            if (wtype == 1 || wtype == 2)
            {
                createBevel(cutpart, gr1p1, gr1p2, gr1p3, grooveprofie);
            }
            if (wtype2 == 1 || wtype2 == 2)
            {
                createBevel(cutpart, gr2p1, gr2p2, gr2p3, grooveprofie);
            }
        }

        private void createBevel(Part cuttedpart, T3D.Point p1, T3D.Point p2, T3D.Point p3, string profile)
        {
            ContourPlate contourPlate = new ContourPlate();
            contourPlate.Class = BooleanPart.BooleanOperativeClassName;
            contourPlate.Profile.ProfileString = profile;
            contourPlate.Contour.AddContourPoint(new ContourPoint(p1, new Chamfer()));
            contourPlate.Contour.AddContourPoint(new ContourPoint(p2, new Chamfer()));
            contourPlate.Contour.AddContourPoint(new ContourPoint(p3, new Chamfer()));
            contourPlate.Name = "PLUGIN";
            ///  if 
            contourPlate.Insert();
            {
                BooleanPart booleanPart = new BooleanPart();
                booleanPart.Father = cuttedpart;
                booleanPart.SetOperativePart(contourPlate);
                try
                {
                    booleanPart.Insert();
                }
                catch
                {
                }
                contourPlate.Delete();
            }
        }
        public T3D.Point getmidpoint(T3D.Point p1, T3D.Point p2)
        {
            double dis = T3D.Distance.PointToPoint(p1, p2);
            T3D.Vector vec = new T3D.Vector(p2 - p1); vec.Normalize();
            return p1 + 0.5 * dis * vec;
        }

        public Weld Weld(Part main, Part sec, double Size, double Size2, int type, int type2, double angle, double angle2, double root, double root2, double throat, double throat2, int ShoporSite, string WeldDirection, string Comment, int topgrind, int topgrind2)
        {
            Weld weld = new Weld();
            weld.MainObject = main;
            weld.SecondaryObject = sec;
            weld.SizeAbove = Size;
            weld.SizeBelow = Size2;
            weld.AngleAbove = angle;
            weld.AngleBelow = angle2;
            weld.RootFaceAbove = root;
            weld.RootFaceBelow = root2;
            weld.EffectiveThroatAbove = throat;
            weld.EffectiveThroatBelow = throat2;
            weld.AroundWeld = false;
            weld.ReferenceText = Comment;
            if (ShoporSite == 0)
            {
                weld.ShopWeld = true;
            }
            else
            {
                weld.ShopWeld = false;
            }
            switch (WeldDirection)
            {
                case "+x": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_PLUS_X; break;
                case "-x": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_MINUS_X; ; break;
                case "+y": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_PLUS_Y; break;
                case "-y": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_MINUS_Y; break;
                case "+z": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_PLUS_Z; break;
                case "-z": weld.Position = Tekla.Structures.Model.Weld.WeldPositionEnum.WELD_POSITION_MINUS_Z; break;
            }
            if (topgrind == 0)
            {
                weld.ContourAbove = BaseWeld.WeldContourEnum.WELD_CONTOUR_NONE;
            }
            else if (topgrind == 1)
            {
                weld.ContourAbove = BaseWeld.WeldContourEnum.WELD_CONTOUR_FLUSH;
            }
            else if (topgrind == 2)
            {
                weld.ContourAbove = BaseWeld.WeldContourEnum.WELD_CONTOUR_CONVEX;
            }
            else
            {
                weld.ContourAbove = BaseWeld.WeldContourEnum.WELD_CONTOUR_CONCAVE;
            }

            if (topgrind2 == 0)
            {
                weld.ContourBelow = BaseWeld.WeldContourEnum.WELD_CONTOUR_NONE;
            }
            else if (topgrind2 == 1)
            {
                weld.ContourBelow = BaseWeld.WeldContourEnum.WELD_CONTOUR_FLUSH;
            }
            else if (topgrind2 == 2)
            {
                weld.ContourBelow = BaseWeld.WeldContourEnum.WELD_CONTOUR_CONVEX;
            }
            else
            {
                weld.ContourBelow = BaseWeld.WeldContourEnum.WELD_CONTOUR_CONCAVE;
            }

            if (type != 0)
            {
                weld.AngleAbove = angle;
            }
            if (type2 != 0)
            {
                weld.AngleBelow = angle2;
            }

            /*(0 = fillet , 1 = grove)*/
            if (type == 1)
            {
                weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_BEVEL_GROOVE_SINGLE_BEVEL_BUTT;
            }
            else if (type == 2)
            {
                weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_PARTIAL_PENETRATION_SINGLE_BEVEL_BUTT_PLUS_FILLET;
            }
            else if (type == 3)
            {
                weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_NONE;
            }
            else
            {
                weld.TypeAbove = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;
            }

            if (type2 == 1)
            {
                weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_BEVEL_GROOVE_SINGLE_BEVEL_BUTT;
            }
            else if (type2 == 2)
            {
                weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_PARTIAL_PENETRATION_SINGLE_BEVEL_BUTT_PLUS_FILLET;
            }
            else if (type2 == 3)
            {
                weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_NONE;
            }
            else
            {
                weld.TypeBelow = BaseWeld.WeldTypeEnum.WELD_TYPE_FILLET;
            }

            try
            {
                weld.Insert();
            }
            catch
            {
            }


            return weld;
        }

    }
}
