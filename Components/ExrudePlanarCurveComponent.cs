using System;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Bella.Components
{
  public class ExrudePlanarCurveComponent : GH_Component
  {
    public ExrudePlanarCurveComponent()
      : base("Extrude Planar", "Extp", "Extrudes planar curves a specified distance", "Surface", "Freeform")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager input)
    {
      input.AddCurveParameter("Curve", "C", "Planar curve to extrude", GH_ParamAccess.item);
      input.AddNumberParameter("Distance", "D", "Extrusion distance", GH_ParamAccess.item, 1.0);
      input.AddBooleanParameter("BothSides", "B", "Extrude curve on both sides", GH_ParamAccess.item, false);
      input.AddBooleanParameter("Solid", "S", "Create a closed solid if curve is closed", GH_ParamAccess.item, false);
      input.AddBooleanParameter("Up", "U", "Extrude in up direction", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager output)
    {
      output.AddBrepParameter("Extrusion", "E", "Extrusion results", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess data)
    {
      var tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
      var angle_tolerance = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;

      // Planar curve to extrude
      Curve curve = null;
      if (!data.GetData(0, ref curve))
        return;

      // Verify curve is planar
      if (!curve.TryGetPlane(out var plane, tolerance))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Curve is not planar");
        return;
      }

      // Extrusion distance
      var distance = RhinoMath.UnsetValue;
      if (!data.GetData(1, ref distance) || IsZero(distance)) return;

      // Extrude curve on both sides
      var both_sides = false;
      if (!data.GetData(2, ref both_sides)) return;

      // Create a closed solid if curve is closed
      var solid = false;
      if (!data.GetData(3, ref solid)) return;

      // Extrude up
      var up = false;
      if (!data.GetData(4, ref up)) return;

      // Extrusion direction
      var normal = plane.Normal;
      normal.Unitize();

      if (up)
      {
        // If normal points downwards, reverse it
        if (IsLessThanZero(plane.ZAxis.Z))
          normal.Reverse();
      }

      // If extruding boths sides, offset the curve in the -normal
      // direction by the specified distance, and then double the
      // distance.
      if (both_sides)
      {
        var xform = Transform.Translation(-normal * distance);
        var offset_curve = curve.DuplicateCurve();
        offset_curve.Transform(xform);
        curve = offset_curve;
        distance *= 2.0;
      }

      // Create a surface by extruding the curve
      var surface = Surface.CreateExtrusion(curve, normal * distance);
      if (null == surface)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Extrusion failed");
        return;
      }

      // Create a Brep from the surface
      var brep = surface.ToBrep();

      // The profile curve is a degree=1 curve. Thus, the extruded surface will
      // have kinks. Because kinked surface can cause problems down stream, Rhino
      // always splits kinked surfaces when adding Breps to the document. Since
      // we are not adding this Brep to the document, lets split the kinked
      // surfaces ourself.
      brep.Faces.SplitKinkyFaces(angle_tolerance, true);

      if (curve.IsClosed && solid)
      {
        // Cap any planar holes
        var capped_brep = brep.CapPlanarHoles(tolerance);
        if (null != capped_brep)
        {
          // The profile curve, created by the input points, is oriented clockwise.
          // Thus when the profile is extruded, the resulting surface will have its
          // normals pointed inwards.
          if (BrepSolidOrientation.Inward == capped_brep.SolidOrientation)
            capped_brep.Flip();

          brep = capped_brep;
        }
      }

      // Set result
      data.SetData(0, brep);
    }

    /// <summary>
    /// Returns true if the input is zero within tolerance
    /// </summary>
    private static bool IsZero(double d)
    {
      return Math.Abs(d) < RhinoMath.ZeroTolerance;
    }

    /// <summary>
    /// Returns true if the input is less than zero within tolerance
    /// </summary>
    private static bool IsLessThanZero(double d)
    {
      return !IsZero(d) && d < -RhinoMath.ZeroTolerance;
    }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override System.Drawing.Bitmap Icon => Properties.Resources.ExtrudePlanarCurve_24x24;

    public override Guid ComponentGuid => new Guid("315066b6-0dbc-4ba3-86c8-8bc4357ef750");
  }
}
