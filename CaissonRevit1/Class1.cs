using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaissonRevit1
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class RevitRein1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection selection = uiDoc.Selection;
            Reference reference = selection.PickObject(ObjectType.Element);
            Element element = doc.GetElement(reference);

            if (element != null)
            {
                if (element.GetType() != typeof(FamilyInstance))
                {
                    TaskDialog.Show("错误", "所选对象不是族实例");
                    return Result.Failed;
                }

                #region 参数读取与设定
                //读取沉井参数        
                XYZ p0 = (element.Location as LocationPoint).Point;
                FamilyInstance fi = element as FamilyInstance;
                double t1 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double t2 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("下部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double t3 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("底板厚度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double length = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double width = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_rjd = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("刃脚顶标高").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_bj = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("变阶处标高").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_jd = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("井顶标高").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_rjdd = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("刃脚底标高").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double b_rjn = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("刃脚内凸宽度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double b_rjw = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("刃脚外凸宽度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_rjw = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("刃脚外凸高度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double h_dbt = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("底板端部上凸高度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double b_dbt = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("底板端部上凸宽度").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET) - b_rjn;

                double angle1 = Math.Atan(h_dbt / b_rjn);
                double angle2 = Math.Atan(h_dbt / b_dbt);
                double angle3 = Math.Atan(h_rjw / b_rjw);
                double la1 = UnitUtils.Convert(200, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET); //延伸长度1

                //设定其他参数
                double tbh1 = UnitUtils.Convert(50, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET); //保护层厚度
                double tbh2 = UnitUtils.Convert(80, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET); //保护层厚度
                RebarHookType hookType135 = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).FirstOrDefault(t => t.Name == "标准 - 135 度") as RebarHookType; //钢筋弯头类型
                RebarHookType hookType90 = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).FirstOrDefault(t => t.Name == "标准 - 90 度") as RebarHookType; //钢筋弯头类型
                #endregion

                #region 创建底板钢筋
                /*
                //X向顶部
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 18;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()) / space1;
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z - tbh2), new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 + tbh2, p0.Z - tbh2));
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建底板钢筋 - X & T");
                    Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType90, hookType90,
                                element, XYZ.BasisY, new List<Curve>(){curve1}, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                    rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                    rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("1");
                    tr.Commit();
                }

                //X向底部
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 18;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()) / space1;

                    IList<XYZ> ps = new List<XYZ>();
                    ps.Add(new XYZ(p0.X - length / 2 + b_rjn + b_dbt-tbh2*Math.Sin(angle2)+ la1 * Math.Cos(angle2), p0.Y - width / 2 + tbh2, p0.Z - tbh2*Math.Cos(angle2)- la1 * Math.Sin(angle2)));
                    ps.Add(new XYZ(p0.X - length / 2 + b_rjn + tbh2/Math.Cos(angle1/2+ angle2 / 2)*Math.Sin(angle1 / 2 - angle2 / 2), p0.Y - width / 2 + tbh2, p0.Z +h_dbt- tbh2 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Cos(angle1 / 2 - angle2 / 2))); 
                    ps.Add(new XYZ(p0.X - length / 2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z - tbh2/Math.Tan(Math.PI/4+angle1/2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z - t3 + tbh2));
                    ps.Add(new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 + tbh2, p0.Z - t3 + tbh2));
                    ps.Add(new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 + tbh2, p0.Z - tbh2 / Math.Tan(Math.PI / 4 + angle1 / 2)));
                    ps.Add(new XYZ(p0.X + length / 2 - b_rjn - tbh2 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Sin(angle1 / 2 - angle2 / 2), p0.Y - width / 2 + tbh2, p0.Z + h_dbt - tbh2 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Cos(angle1 / 2 - angle2 / 2)));
                    ps.Add(new XYZ(p0.X + length / 2 - b_rjn - b_dbt + tbh2 * Math.Sin(angle2) - la1 * Math.Cos(angle2), p0.Y - width / 2 + tbh2, p0.Z - tbh2 * Math.Cos(angle2) - la1 * Math.Sin(angle2)));

                    IList<Curve> curves1 = new List<Curve>();
                    for(int i=0; i < ps.Count-1; i++) {
                        curves1.Add(Line.CreateBound(ps[i], ps[i+1]));
                    }
                    
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建底板钢筋 - X & B");
                    Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                element, XYZ.BasisY, curves1, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                    rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                    rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("2");
                    tr.Commit();
                }

                //Y向顶部
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 20;
                    int space1 = 100;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / space1;
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 + tbh1, p0.Y - width / 2 + tbh1, p0.Z - tbh1), new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 - tbh1, p0.Z - tbh1));
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建底板钢筋 - Y & T");
                    Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType90, hookType90,
                                element, XYZ.BasisX, new List<Curve>() { curve1 }, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                    rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                    rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("3");
                    tr.Commit();
                }

                //Y向底部
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 18;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / space1;

                    IList<XYZ> ps = new List<XYZ>();
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y - width / 2 + b_rjn + b_dbt - tbh1 * Math.Sin(angle2) + la1 * Math.Cos(angle2), p0.Z - tbh1 * Math.Cos(angle2) - la1 * Math.Sin(angle2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y - width / 2 + b_rjn + tbh1 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Sin(angle1 / 2 - angle2 / 2), p0.Z + h_dbt - tbh1 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Cos(angle1 / 2 - angle2 / 2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y - width / 2 + tbh1, p0.Z - tbh1 / Math.Tan(Math.PI / 4 + angle1 / 2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y - width / 2 + tbh1, p0.Z - t3 + tbh1));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 - tbh1, p0.Z - t3 + tbh1));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 - tbh1, p0.Z - tbh1 / Math.Tan(Math.PI / 4 + angle1 / 2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 - b_rjn - tbh1 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Sin(angle1 / 2 - angle2 / 2), p0.Z + h_dbt - tbh1 / Math.Cos(angle1 / 2 + angle2 / 2) * Math.Cos(angle1 / 2 - angle2 / 2)));
                    ps.Add(new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 - b_rjn - b_dbt + tbh1 * Math.Sin(angle2) - la1 * Math.Cos(angle2), p0.Z - tbh1 * Math.Cos(angle2) - la1 * Math.Sin(angle2)));

                    IList<Curve> curves1 = new List<Curve>();
                    for (int i = 0; i < ps.Count - 1; i++)
                    {
                        curves1.Add(Line.CreateBound(ps[i], ps[i + 1]));
                    }

                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建底板钢筋 - Y & B");
                    Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                element, XYZ.BasisX, curves1, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                    rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                    rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("4");
                    tr.Commit();
                }
                */
                #endregion

                #region 创建刃脚钢筋
                
                //刃脚外侧水平
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 25;
                    double space1 = 200;
                    int n1 = (int)Math.Floor((Int32.Parse(fi.Symbol.LookupParameter("刃脚底标高").AsValueString()) - Int32.Parse(fi.Symbol.LookupParameter("底板厚度").AsValueString())) / space1);
                    XYZ p1 = new XYZ(p0.X - length / 2 - t2 - b_rjw + tbh2, p0.Y - width / 2 - t2 - b_rjw + tbh2, p0.Z - h_rjdd + tbh2);
                    XYZ p2 = new XYZ(p0.X - length / 2 - t2 - b_rjw + tbh2, p0.Y + width / 2 + t2 + b_rjw - tbh2, p0.Z - h_rjdd + tbh2);
                    XYZ p3 = new XYZ(p0.X + length / 2 + t2 + b_rjw - tbh2, p0.Y + width / 2 + t2 + b_rjw - tbh2, p0.Z - h_rjdd + tbh2);
                    XYZ p4 = new XYZ(p0.X + length / 2 + t2 + b_rjw - tbh2, p0.Y - width / 2 - t2 - b_rjw + tbh2, p0.Z - h_rjdd + tbh2);
                    IList<Curve> curves1 = new List<Curve>() { Line.CreateBound(p1,p2), Line.CreateBound(p2, p3), Line.CreateBound(p3, p4), Line.CreateBound(p4, p1) };
                    RebarBarType barType1 = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建刃脚外侧水平钢筋");
                    foreach(Curve curve in curves1)
                    {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType1, hookType90, hookType90,
                                element, XYZ.BasisZ, new List<Curve>() { curve }, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("5");
                    }
                    tr.Commit();

                    int r2 = 25;
                    double space2 = 200;
                    int n2 = (int)Math.Floor((Int32.Parse(fi.Symbol.LookupParameter("刃脚底标高").AsValueString()) - Int32.Parse(fi.Symbol.LookupParameter("底板厚度").AsValueString())) / space2);
                    double la2 = UnitUtils.Convert(Math.Ceiling(Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / 3.0 / 100) * 100, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    IList<Curve> curves2 = new List<Curve>() { Line.CreateBound(p1 + new XYZ(la2 + t2 + b_rjw, 0, space1/304.8/2), p1 + new XYZ(0, 0, space1 / 304.8 / 2)),
                                                               Line.CreateBound(p1 + new XYZ(0, 0, space1 / 304.8 / 2), p2 + new XYZ(0, 0, space1 / 304.8 / 2)),
                                                               Line.CreateBound(p2 + new XYZ(0, 0, space1 / 304.8 / 2), p2 + new XYZ(la2 + t2 + b_rjw, 0, space1 / 304.8 / 2)) };
                    IList<Curve> curves3 = new List<Curve>() { Line.CreateBound(p3 + new XYZ(-la2 - t2 - b_rjw, 0, space1 / 304.8 / 2), p3 + new XYZ(0, 0, space1 / 304.8 / 2)),
                                                               Line.CreateBound(p3 + new XYZ(0, 0, space1 / 304.8 / 2), p4 + new XYZ(0, 0, space1 / 304.8 / 2)),
                                                               Line.CreateBound(p4 + new XYZ(0, 0, space1 / 304.8 / 2), p4 + new XYZ(-la2 - t2 - b_rjw, 0, space1 / 304.8 / 2)) };
                    RebarBarType barType2 = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r2.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建刃脚外侧水平附加钢筋");
                    foreach (IList<Curve> curves in new List<IList<Curve>>() { curves2, curves3 })
                    {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType2, hookType135, hookType135,
                                element, XYZ.BasisZ, curves, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space2, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("5a");
                    }
                    tr.Commit();
                }
                
                
                //刃脚外侧竖向
                using (Transaction tr = new Transaction(doc))
                {                    
                    int r1 = 22;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()) / space1;
                    int n2 = Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / space1;
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 - b_rjw + tbh1, p0.Y - width / 2 + tbh1, p0.Z - h_rjdd + tbh1), 
                                                    new XYZ(p0.X - length / 2 - t2 - b_rjw + tbh1, p0.Y - width / 2 + tbh1, p0.Z - t3- tbh1/Math.Tan(Math.PI/4+angle3/2)));
                    Curve curve2 = Line.CreateBound(new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 + t2 + b_rjw - tbh1, p0.Z - h_rjdd + tbh1), 
                                                    new XYZ(p0.X - length / 2 + tbh1, p0.Y + width / 2 + t2 + b_rjw - tbh1, p0.Z - t3 - tbh1 / Math.Tan(Math.PI / 4 + angle3 / 2)));
                    Curve curve3 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 + b_rjw - tbh1, p0.Y + width / 2 - tbh1, p0.Z - h_rjdd + tbh1), 
                                                    new XYZ(p0.X + length / 2 + t2 + b_rjw - tbh1, p0.Y + width / 2 - tbh1, p0.Z - t3 - tbh1 / Math.Tan(Math.PI / 4 + angle3 / 2)));
                    Curve curve4 = Line.CreateBound(new XYZ(p0.X + length / 2 - tbh1, p0.Y - width / 2 - t2 - b_rjw + tbh1, p0.Z - h_rjdd + tbh1), 
                                                    new XYZ(p0.X + length / 2 - tbh1, p0.Y - width / 2 - t2 - b_rjw + tbh1, p0.Z - t3 - tbh1 / Math.Tan(Math.PI / 4 + angle3 / 2)));
                    IList<Curve> curves = new List<Curve>() { curve1, curve2, curve3, curve4 };
                    IList<XYZ> norms = new List<XYZ>() { XYZ.BasisY, XYZ.BasisX, -XYZ.BasisY, -XYZ.BasisX };
                    IList<int> nums = new List<int>() { n1, n2, n1, n2 };
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建刃脚外侧竖向钢筋");
                    for(int i=0;i<4;i++) {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                element, norms[i], new List<Curve>() { curves[i] }, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(nums[i], UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("14");
                    }
                    tr.Commit();
                }

                #endregion

                #region 创建下阶壁板钢筋
                //下阶壁板外侧水平
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 22;
                    double space1 = 200;
                    int n1 = (int)Math.Floor((Int32.Parse(fi.Symbol.LookupParameter("变阶处标高").AsValueString()) + Int32.Parse(fi.Symbol.LookupParameter("刃脚顶标高").AsValueString()) + Int32.Parse(fi.Symbol.LookupParameter("底板厚度").AsValueString())) / space1);
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z - t3 + tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z - t3 + tbh1));
                    Curve curve2 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z - t3 + tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z - t3 + tbh1));
                    Curve curve3 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z - t3 + tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z - t3 + tbh1));
                    Curve curve4 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z - t3 + tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z - t3 + tbh1));
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建下阶壁板外侧水平钢筋");
                    foreach(Curve curve in new List<Curve>() { curve1, curve2, curve3, curve4 })
                    {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType90, hookType90,
                                element, XYZ.BasisZ, new List<Curve>() { curve }, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("9");
                    }
                    tr.Commit();
                }
                /*
                //下阶壁板外侧竖向
                using (Transaction tr = new Transaction(doc))
                {                    
                    int r1 = 20;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()) / space1;
                    int n2 = Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / space1;
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X - length / 2 - t2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve2 = Line.CreateBound(new XYZ(p0.X - length / 2 + tbh2, p0.Y + width / 2 + t2 - tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X - length / 2 + tbh2, p0.Y + width / 2 + t2 - tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve3 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh2, p0.Y + width / 2 - tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X + length / 2 + t2 - tbh2, p0.Y + width / 2 - tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve4 = Line.CreateBound(new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 - t2 + tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 - t2 + tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    IList<Curve> curves = new List<Curve>() { curve1, curve2, curve3, curve4 };
                    IList<XYZ> norms = new List<XYZ>() { XYZ.BasisY, XYZ.BasisX, -XYZ.BasisY, -XYZ.BasisX };
                    IList<int> nums = new List<int>() { n1, n2, n1, n2 };
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建上阶壁板外侧竖向钢筋");
                    for(int i=0;i<4;i++) {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                element, norms[i], new List<Curve>() { curves[i] }, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(nums[i], UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("14");
                    }
                    tr.Commit();
                }
                */
                #endregion

                #region 创建上阶壁板钢筋
                //上阶壁板外侧水平
                /*
                using (Transaction tr = new Transaction(doc))
                {
                    int r1 = 18;
                    double space1 = 150;
                    int n1 = (int)Math.Floor((Int32.Parse(fi.Symbol.LookupParameter("井顶标高").AsValueString()) - Int32.Parse(fi.Symbol.LookupParameter("变阶处标高").AsValueString())) / space1);
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z + h_rjd + h_bj + tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z + h_rjd + h_bj + tbh1));
                    Curve curve2 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z + h_rjd + h_bj + tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z + h_rjd + h_bj + tbh1));
                    Curve curve3 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, p0.Z + h_rjd + h_bj + tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z + h_rjd + h_bj + tbh1));
                    Curve curve4 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z + h_rjd + h_bj + tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, p0.Z + h_rjd + h_bj + tbh1));
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建上阶壁板外侧水平钢筋");
                    foreach(Curve curve in new List<Curve>() { curve1, curve2, curve3, curve4 })
                    {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, hookType90, hookType90,
                                element, XYZ.BasisZ, new List<Curve>() { curve }, RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(n1, UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("13");
                    }
                    tr.Commit();
                }

                //上阶壁板外侧竖向
                using (Transaction tr = new Transaction(doc))
                {                    
                    int r1 = 20;
                    int space1 = 150;
                    int n1 = Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()) / space1;
                    int n2 = Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()) / space1;
                    Curve curve1 = Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X - length / 2 - t2 + tbh2, p0.Y - width / 2 + tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve2 = Line.CreateBound(new XYZ(p0.X - length / 2 + tbh2, p0.Y + width / 2 + t2 - tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X - length / 2 + tbh2, p0.Y + width / 2 + t2 - tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve3 = Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh2, p0.Y + width / 2 - tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X + length / 2 + t2 - tbh2, p0.Y + width / 2 - tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    Curve curve4 = Line.CreateBound(new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 - t2 + tbh2, p0.Z + h_rjd + h_bj - 800 / 304.8), new XYZ(p0.X + length / 2 - tbh2, p0.Y - width / 2 - t2 + tbh2, p0.Z + h_rjd + h_jd - tbh2));
                    IList<Curve> curves = new List<Curve>() { curve1, curve2, curve3, curve4 };
                    IList<XYZ> norms = new List<XYZ>() { XYZ.BasisY, XYZ.BasisX, -XYZ.BasisY, -XYZ.BasisX };
                    IList<int> nums = new List<int>() { n1, n2, n1, n2 };
                    RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == r1.ToString() + " HRB400") as RebarBarType;

                    tr.Start("创建上阶壁板外侧竖向钢筋");
                    for(int i=0;i<4;i++) {
                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                element, norms[i], new List<Curve>() { curves[i] }, RebarHookOrientation.Left, RebarHookOrientation.Left, true, true);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(nums[i], UnitUtils.Convert(space1, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET), true, true, true);
                        rebar.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM).Set("14");
                    }
                    tr.Commit();
                }
                */
                #endregion

                return Result.Succeeded;
            }
            return Result.Succeeded;
        }
    }

    [TransactionAttribute(TransactionMode.Manual)]
    public class RevitReinTag1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            Selection selection = uiDoc.Selection;

            using (Transaction tr = new Transaction(doc))
            {
                tr.Start("创建钢筋注释");

                Autodesk.Revit.DB.View view = uiDoc.ActiveView;//当前活动视图
                Reference refer1 = selection.PickObject(ObjectType.Element, "请选择要标注的钢筋:");
                Rebar rebar1 = doc.GetElement(refer1) as Rebar;
                IList<Subelement> subelements = rebar1.GetSubelements(); // here are the subelements
                IList<Curve> curves = rebar1.GetCenterlineCurves(true, false, false, MultiplanarOption.IncludeAllMultiplanarCurves,0);
                Curve curve = curves[0];
                //TaskDialog.Show("111", curve.Length.ToString());
                //Add the tag to the middle of duct
                //Curve curve = rebar1.GetCenterlineCurves(true, false, false,MultiplanarOption.IncludeAllMultiplanarCurves,0) as Curve;
                
                XYZ rebarMid = curve.Evaluate(0.5, true);
                IndependentTag tag = IndependentTag.Create(doc,view.Id, refer1, false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Vertical, rebarMid);
                tag.get_Parameter(BuiltInParameter.LEADER_LINE).Set(1);
                tag.TagHeadPosition = tag.TagHeadPosition.Add(new XYZ(-1, 0, 2));

                //
                //tag.LeaderElbow = tag.LeaderElbow + new XYZ(0,0,-5);
                //TaskDialog.Show("111", tag.HasLeader.ToString());

                //tag.TagHeadPosition.Add(new XYZ(0, -6, 0));

                /*
                foreach (var subelement in subelements)
                {
                    IndependentTag tag1 = IndependentTag.Create(doc, view.Id, subelement.GetReference(), 
                        true, Autodesk.Revit.DB.TagMode.TM_ADDBY_CATEGORY, Autodesk.Revit.DB.TagOrientation.Horizontal, new XYZ(0,0,0));
                }
                */

                tr.Commit();
                
            }

            return Result.Succeeded;
        }
    }


        /*
        [TransactionAttribute(TransactionMode.Manual)]
        public class RevitTry1 : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                Selection selection = uiDoc.Selection;
                Reference reference = selection.PickObject(ObjectType.Element);
                Element element = doc.GetElement(reference);
                //object obj = element.GetGeometryObjectFromReference(reference);
                //Face face = obj as Face;
                if (element != null)
                {
                    if (element.GetType() != typeof(FamilyInstance))
                    {
                        TaskDialog.Show("错误", "所选对象不是族实例");
                        return Result.Failed;
                    }

                    //ViewPlan viewPlan = doc.ActiveView as ViewPlan;
                    //if (viewPlan == null)
                    //{
                    //    TaskDialog.Show("错误", "不是viewPlan");
                    //    return Result.Failed;
                    //}

                    //CurveArray curveArray1 = new CurveArray();
                    //curveArray1.Append(Line.CreateBound(new XYZ(p0.X-length/2-t2+tbh1, p0.Y - width / 2 - t2 + tbh1, 0), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, 0)));
                    //curveArray1.Append(Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, 0), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, 0)));
                    //doc.Create.NewDetailCurveArray(viewPlan, curveArray1);

                    XYZ p0 = (element.Location as LocationPoint).Point;
                    FamilyInstance fi = element as FamilyInstance;
                    double t1 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double t2 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("下部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double length = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double width = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    Level LevelDBBG = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().FirstOrDefault(t => t.Name == "DBBG") as Level;
                    double DBBG = LevelDBBG.Elevation;
                    //double BJBG = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    //double DBG = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    //double RDBG = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);

                    double tbh1 = UnitUtils.Convert(50, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET); // 保护层厚度
                    TaskDialog.Show("信息", DBBG.ToString());
                }
                return Result.Succeeded;
            }
        }

        [TransactionAttribute(TransactionMode.Manual)]
        public class RevitTry2 : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;
                Selection selection = uiDoc.Selection;
                Reference reference = selection.PickObject(ObjectType.Element);
                Element element = doc.GetElement(reference);

                if (element != null)
                {
                    if(element.GetType()!=typeof(FamilyInstance))
                    {
                        TaskDialog.Show("错误", "所选对象不是族实例");
                        return Result.Failed;
                    }

                    XYZ p0 = (element.Location as LocationPoint).Point;
                    FamilyInstance fi = element as FamilyInstance;
                    double t1 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("上部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double t2 = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("下部壁厚").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double length = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净长").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    double width = UnitUtils.Convert(Int32.Parse(fi.Symbol.LookupParameter("沉井净宽").AsValueString()), DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                    Level LevelDBBG = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().FirstOrDefault(t => t.Name == "DBBG") as Level;
                    Level LevelBJBG = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().FirstOrDefault(t => t.Name == "BJBG") as Level;
                    Level LevelDBG = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().FirstOrDefault(t => t.Name == "DBG") as Level;
                    Level LevelRDBG = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().FirstOrDefault(t => t.Name == "RDBG") as Level;
                    double DBBG = LevelDBBG.Elevation;
                    double BJBG = LevelBJBG.Elevation;
                    double DBG = LevelDBG.Elevation;
                    double RDBG = LevelRDBG.Elevation;

                    double tbh1 = UnitUtils.Convert(50, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET); // 保护层厚度
                    //TaskDialog.Show("信息", DBBG.ToString());

                    using (Transaction tr = new Transaction(doc))
                    {
                        tr.Start("Create Rein");

                        IList<Curve> curves = new List<Curve>();
                        curves.Add(Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, DBBG - tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, DBBG - tbh1)));
                        curves.Add(Line.CreateBound(new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y + width / 2 + t2 - tbh1, DBBG - tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, DBBG - tbh1)));
                        curves.Add(Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y + width / 2 + t2 - tbh1, DBBG - tbh1), new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, DBBG - tbh1)));
                        curves.Add(Line.CreateBound(new XYZ(p0.X + length / 2 + t2 - tbh1, p0.Y - width / 2 - t2 + tbh1, DBBG - tbh1), new XYZ(p0.X - length / 2 - t2 + tbh1, p0.Y - width / 2 - t2 + tbh1, DBBG - tbh1)));

                        RebarBarType barType = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).FirstOrDefault(t => t.Name == "20 HRB400") as RebarBarType;
                        RebarHookType hookType135 = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).FirstOrDefault(t => t.Name == "标准 - 135 度") as RebarHookType;
                        RebarHookType hookType90 = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).FirstOrDefault(t => t.Name == "标准 - 90 度") as RebarHookType;
                        RebarShape barShape1 = new FilteredElementCollector(doc).OfClass(typeof(RebarShape)).FirstOrDefault(t => t.Name == "34") as RebarShape;

                        Rebar rebar = Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null,
                                    element, new XYZ(0,0,1), curves, RebarHookOrientation.Right, RebarHookOrientation.Left, true, true);
                        rebar.GetShapeDrivenAccessor().SetRebarShapeId(barShape1.Id);
                        rebar.GetShapeDrivenAccessor().SetLayoutAsNumberWithSpacing(20, UnitUtils.Convert(200, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET),true,true,true);

                        tr.Commit();
                    }
                    return Result.Succeeded;
                }
                return Result.Succeeded;
            }
        }
        */
    }
