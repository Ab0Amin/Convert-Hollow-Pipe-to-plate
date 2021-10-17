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
    }
}
