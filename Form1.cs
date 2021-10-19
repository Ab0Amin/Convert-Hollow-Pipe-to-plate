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
            //Picker input = new Picker();
            //Part pipe = input.PickObject(Picker.PickObjectEnum.PICK_ONE_PART) as Part;
       Tekla.Structures.Model.UI.ModelObjectSelector ms = new Tekla.Structures.Model.UI.ModelObjectSelector();
            //=     myModel.GetModelObjectSelector();
   ModelObjectEnumerator me =    ms.GetSelectedObjects();
   while (me.MoveNext())
   {
       Part pipe = me.Current as Part;
       convert(pipe);
       myModel.CommitChanges();
   }
          



        }

        private void convert(Part pipe)
        {
            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());
            PolyBeam polyPipe = pipe as PolyBeam;
            List<Beam> beams = new List<Beam>();
            #region poly
            if (polyPipe != null)
            {
                ArrayList points = polyPipe.GetCenterLine(false);
                //double counter = 0;
                Vector vecZ = new Vector(); ;
                T3D.Point RPoint = new T3D.Point();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    double fac = 1, fac2 = 1;

                    if (i == 0)
                    {
                        fac2 = 0;
                    }
                    else if (i == points.Count - 2)
                    {
                        fac = 0;
                    }
                    Vector X = new Vector((points[i + 1] as T3D.Point) - (points[i] as T3D.Point));
                    X.Normalize();
                    Beam bean = new Beam();
                    bean.Profile.ProfileString = polyPipe.Profile.ProfileString;
                    bean.Position.Depth = Position.DepthEnum.MIDDLE;
                    bean.Position.Plane = Position.PlaneEnum.MIDDLE;
                    bean.AssemblyNumber = polyPipe.AssemblyNumber;
                    bean.PartNumber = polyPipe.PartNumber;
                    bean.Material = polyPipe.Material;
                    bean.Name = polyPipe.Name;
                    bean.StartPoint = (points[i] as T3D.Point) - 1000 * X * fac2;
                    bean.EndPoint = (points[i + 1] as T3D.Point) + 1000 * X * fac;

                    bean.Insert();

                    beams.Add(bean);

                    //cutTwoSides(points, ref counter, ref vecZ, ref RPoint, i, bean);
                    //if (i != 0 && i != points.Count - 2)
                    {
                        //if (counter == 0)
                        {
                            if (points.Count - 1 >= i + 2)
                            {
                                T3D.Point p1 = points[i] as T3D.Point;
                                T3D.Point p2 = points[i + 1] as T3D.Point;
                                T3D.Point p3 = points[i + 2] as T3D.Point;
                                if (p1 != null && p2 != null && p3 != null)
                                {
                                    calcPlane(p1, p2, p3, out vecZ, out RPoint);
                                    inclintCut(bean, p2, 1, vecZ, RPoint);
                                    //counter++;
                                }
                            }
                        }
                        //else if (counter == 1)
                        {
                            if (points.Count - 1 >= i + 1 && i - 1 >= 0)
                            {
                                T3D.Point p1 = points[i - 1] as T3D.Point;
                                T3D.Point p2 = points[i] as T3D.Point;
                                T3D.Point p3 = points[i + 1] as T3D.Point;
                                if (p1 != null && p2 != null && p3 != null)
                                {
                                    calcPlane(p1, p2, p3, out vecZ, out RPoint);
                                    inclintCut(bean, p2, -1, vecZ, RPoint);
                                    //counter = 0;
                                }
                            }
                        }

                    }
                }


            }
            for (int i = 0; i < beams.Count; i++)
            {
                ConvertPipeToPlate(beams[i]);
                beams[i].Delete();
                //i--;
            }
            #endregion

            Beam beamPipe = pipe as Beam;
            if (beamPipe != null)
            {
                ConvertPipeToPlate(pipe);
            }

            if (checkBox1.Checked)
            {
                pipe.Delete();
            }
            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane());

        }

    

        private void inclintCut(Beam bean, T3D.Point p2, double factor, Vector vecZ, T3D.Point RotatedPoint)
        {
         

            Vector cutVec = new Vector(p2 - RotatedPoint);
            cutVec.Normalize();

            Plane cutplane = new Plane();
            cutplane.Origin = p2;
            cutplane.AxisX = cutVec;
            cutplane.AxisY = vecZ*factor;
            CutPlane cut = new CutPlane();
            cut.Plane = cutplane;
            cut.Father = bean;
            cut.Insert();
            
        }

        private void calcPlane(T3D.Point p1, T3D.Point p2, T3D.Point p3, out Vector vecZ, out T3D.Point RotatedPoint)
        {
            Vector vecX = new Vector(p2 - p1);
            Vector vecY = new Vector(p2 - p3);
            vecX.Normalize();
            vecY.Normalize();
            vecZ = vecX.Cross(vecY);
            vecZ.Normalize();
            GeometricPlane plane = new GeometricPlane(p2, vecX, vecZ);
            T3D.Point projected = Projection.PointToPlane(p3, plane);
            double l1 = Distance.PointToPoint(p2, p3);
            double l2 = Distance.PointToPoint(p2, projected);
            // l2/l1 cos
            bool more45 = false;
            double dis1 = Distance.PointToPoint(p1, p2);
            double dis2 = Distance.PointToPoint(projected, p1);
            double dis3 = Distance.PointToPoint(projected, p2);
            if (dis1 < dis2 || dis1 < dis3)
            {
                more45 = true; 
            }
            TransformationPlane current = myModel.GetWorkPlaneHandler().GetCurrentTransformationPlane();
            TransformationPlane transPlane = new TransformationPlane(p2, vecX, vecY);
            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(transPlane);

            T3D.Point p11 = transPlane.TransformationMatrixToLocal.Transform(current.TransformationMatrixToGlobal.Transform(p1));
            double angle = Math.Acos(l2 / l1);
            double angleDegree = angle * 180 / Math.PI;
            if (more45)
            {
                angleDegree = 180 - angleDegree; 
            }
            Matrix mat = MatrixFactory.Rotate((angleDegree / 2) * Math.PI/ 180 , -1 * vecZ);

            RotatedPoint = mat.Transform(p11);
            RotatedPoint = current.TransformationMatrixToLocal.Transform(transPlane.TransformationMatrixToGlobal.Transform(RotatedPoint));

            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(current);
        }
        public Beam check_with_beam(T3D.Point start_point, T3D.Point end_point)
        {
            Beam toe_plate = new Beam();
            toe_plate.StartPoint = start_point;
            toe_plate.EndPoint = end_point;
            toe_plate.Profile.ProfileString = "ROD100";
            toe_plate.Position.Depth = Position.DepthEnum.FRONT;
            toe_plate.Position.Plane = Position.PlaneEnum.RIGHT;
            toe_plate.Position.Rotation = Position.RotationEnum.TOP;
            toe_plate.Insert();
            return toe_plate;
        }

        private void ConvertPipeToPlate(Part pipe)
        {
            CoordinateSystem co = pipe.GetCoordinateSystem();
            ArrayList centerPoints = pipe.GetCenterLine(true);
            Vector vecy = new Vector((centerPoints[1] as T3D.Point) - (centerPoints[0] as T3D.Point));
            vecy.Normalize();
            myModel.GetWorkPlaneHandler().SetCurrentTransformationPlane(new TransformationPlane((centerPoints[0] as T3D.Point), vecy, co.AxisY));
            double length = 200, thickness = 10, diam = 1000;
            pipe.GetReportProperty("PROFILE.PLATE_THICKNESS", ref thickness);
            pipe.GetReportProperty("HEIGHT", ref diam);
            pipe.GetReportProperty("LENGTH_NET", ref length);
            Solid solid = pipe.GetSolid(Solid.SolidCreationTypeEnum.NORMAL);
            T3D.Point min = solid.MinimumPoint;
            T3D.Point zero = new T3D.Point(min.X, 0, 0);
            if (!checkBox3.Checked)
            {
                thickness = double.Parse(tx_thik.Text);
            }
            PolyBeam plate = polyPlate(pipe, length, thickness, diam, zero, double.Parse(tx_groove.Text));
            ModelObjectEnumerator mo = pipe.GetBooleans();
            while (mo.MoveNext())
            {
                CutPlane cut = mo.Current as CutPlane;
                if (cut != null)
                {
                    CutPlane plateCut = new CutPlane();
                    plateCut.Father = plate;
                    plateCut.Plane = cut.Plane;
                    plateCut.Insert();
                }

            }
        }

        private static PolyBeam polyPlate(Part pipe, double length, double thickness, double diam, T3D.Point zeroPoint,double grooveSize)
        {
            PolyBeam plate = new PolyBeam();
            plate.Profile.ProfileString = "PL" + length + "*" + thickness;
            plate.Material = pipe.Material;
            plate.AssemblyNumber = pipe.AssemblyNumber;
            plate.PartNumber = pipe.PartNumber;
            plate.Name = pipe.Name;
            plate.Position.Depth = Position.DepthEnum.BEHIND;
            plate.Position.Rotation = Position.RotationEnum.FRONT;
            plate.Position.Plane = Position.PlaneEnum.LEFT;
            ArrayList contour_points = new ArrayList();
            Vector Z = new Vector(0, 0, 1);
            Vector Y = new Vector(0, 1, 0);
            Vector X = new Vector(1, 0, 0);
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


            groovebetween2points(plate, thickness, p1, p1 + length * X, -1 * Z, -1 * Y, grooveSize, grooveSize, 1, 1, 45.0, 45.0, 5.0);
            Weld(plate, plate, grooveSize, grooveSize, 1, 1, 45, 45, 0, 0, 0, 0, 0, "x", "", 0, 0);
  
            return plate;
        }

        private static void groovebetween2points(Part cutpart, double cutthk, T3D.Point p1, T3D.Point p2, T3D.Vector normalvec, T3D.Vector insidevec, double wsize, double wsize2, int wtype, int wtype2, double wangle, double wangle2, double extension)
        {
            normalvec.Normalize();
            insidevec.Normalize();
            T3D.GeometricPlane faceplane = new T3D.GeometricPlane(p1, new T3D.Vector(p2 - p1), normalvec);
            // T3D.Vector insidevec = new T3D.Vector(stiffmidpoint - T3D.Projection.PointToPlane(stiffmidpoint, faceplane));
            insidevec.Normalize();

            T3D.Point mymidpoint = getmidpoint(p1, p2);

            T3D.Point gr1p1 = mymidpoint;
            T3D.Point gr1p2 = gr1p1 - wsize * normalvec;
            T3D.Point gr1p3 = gr1p1 + wsize * Math.Tan(wangle * Math.PI / 180) * insidevec;

            T3D.Point gr2p1 = mymidpoint ;
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

        private static void createBevel(Part cuttedpart, T3D.Point p1, T3D.Point p2, T3D.Point p3, string profile)
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
        public static T3D.Point getmidpoint(T3D.Point p1, T3D.Point p2)
        {
            double dis = T3D.Distance.PointToPoint(p1, p2);
            T3D.Vector vec = new T3D.Vector(p2 - p1); vec.Normalize();
            return p1 + 0.5 * dis * vec;
        }

        public static Weld Weld(Part main, Part sec, double Size, double Size2, int type, int type2, double angle, double angle2, double root, double root2, double throat, double throat2, int ShoporSite, string WeldDirection, string Comment, int topgrind, int topgrind2)
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

     

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                tx_thik.Enabled = false;
            }
            else
            {
                tx_thik.Enabled = true;
            }
        }


    }
}
