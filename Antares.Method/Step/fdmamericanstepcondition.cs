// C# code for FdmAmericanStepCondition.cs

using Antares.Math;
using Antares.Method.Mesh;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method
{
    /// <summary>
    /// Base interface for a step condition in an FDM scheme.
    /// </summary>
    /// <typeparam name="TArray">The type of the array used in the scheme.</typeparam>
    public interface IStepCondition<TArray> where TArray : class
    {
        void ApplyTo(TArray a, double t);
    }
}

namespace Antares.Method.Step
{
    /// <summary>
    /// Step condition for applying the American exercise constraint in an FDM scheme.
    /// </summary>
    /// <remarks>
    /// At each time step, this condition ensures that the option value
    /// is at least its intrinsic value: V(t,x) = max(V_continuation(t,x), V_intrinsic(t,x)).
    /// </remarks>
    public class FdmAmericanStepCondition : IStepCondition<Array>
    {
        private readonly FdmMesher _mesher;
        private readonly FdmInnerValueCalculator _calculator;

        /// <summary>
        /// Initializes a new instance of the FdmAmericanStepCondition class.
        /// </summary>
        /// <param name="mesher">The FDM mesher, which provides the grid layout.</param>
        /// <param name="calculator">The calculator for the option's intrinsic value.</param>
        public FdmAmericanStepCondition(FdmMesher mesher, FdmInnerValueCalculator calculator)
        {
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        }

        /// <summary>
        /// Applies the American exercise constraint to the solution array.
        /// </summary>
        /// <param name="a">The solution array at the current time step.</param>
        /// <param name="t">The current time.</param>
        public void ApplyTo(Array a, double t)
        {
            QL.Require(((FdmLinearOpLayout)_mesher.Layout).Size == a.Count, 
                      "Inconsistent array dimensions.");

            // The loop iterates over all points in the multi-dimensional grid.
            foreach (var iter in (FdmLinearOpLayout)_mesher.Layout)
            {
                double innerValue = _calculator.InnerValue(iter, t);
                int index = (int)iter.Index;

                if (innerValue > a[index])
                {
                    a[index] = innerValue;
                }
            }
        }
    }
}