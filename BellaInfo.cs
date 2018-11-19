using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Bella
{
  public class BellaInfo : GH_AssemblyInfo
  {
    public override string Name => "Bella";

    public override Bitmap Icon => Properties.Resources.Bella_24x24;

    public override string Description => "Bella component for Grasshopper®";

    public override Guid Id => new Guid("765a4d39-9719-4ce4-8019-88d459842433");

    public override string AuthorName => "Dale Fugier";

    public override string AuthorContact => "https://github.com/dalefugier";
  }
}
