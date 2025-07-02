// C# code for FdmSolverDesc.cs

using Antares.Method.Mesh;
using Antares.Method.Step;
using Antares.Method.Utilities;

namespace Antares.Method.Solver
{
    /// <summary>
    /// Describes the components and parameters for a finite-difference model solver.
    /// </summary>
    public class FdmSolverDesc
    {
        public FdmMesher Mesher { get; }
        public FdmBoundaryConditionSet BcSet { get; }
        public FdmStepConditionComposite Condition { get; }
        public IFdmInnerValueCalculator Calculator { get; }
        public double Maturity { get; }
        public int TimeSteps { get; }
        public int DampingSteps { get; }

        public FdmSolverDesc(
            FdmMesher mesher,
            FdmBoundaryConditionSet bcSet,
            FdmStepConditionComposite condition,
            IFdmInnerValueCalculator calculator,
            double maturity,
            int timeSteps,
            int dampingSteps)
        {
            Mesher = mesher;
            BcSet = bcSet;
            Condition = condition;
            Calculator = calculator;
            Maturity = maturity;
            TimeSteps = timeSteps;
            DampingSteps = dampingSteps;
        }
    }
}