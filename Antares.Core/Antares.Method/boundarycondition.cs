// C# code for BoundaryCondition.cs

using System;

namespace Antares.Method
{
    /// <summary>
    /// Abstract boundary condition class for finite difference problems
    /// </summary>
    /// <typeparam name="TOperator">The type of operator this boundary condition applies to</typeparam>
    public abstract class BoundaryCondition<TOperator>
    {
        /// <summary>
        /// Specifies which side of the domain the boundary condition applies to
        /// </summary>
        public enum Side
        {
            None,
            Upper, 
            Lower
        }

        /// <summary>
        /// This method modifies an operator L before it is applied to an array u 
        /// so that v = Lu will satisfy the given condition.
        /// </summary>
        public abstract void ApplyBeforeApplying(TOperator operatorL);

        /// <summary>
        /// This method modifies an array u so that it satisfies the given condition.
        /// </summary>
        public abstract void ApplyAfterApplying(Array u);

        /// <summary>
        /// This method modifies an operator L before the linear system Lu' = u is solved 
        /// so that u' will satisfy the given condition.
        /// </summary>
        public abstract void ApplyBeforeSolving(TOperator operatorL, Array rhs);

        /// <summary>
        /// This method modifies an array u so that it satisfies the given condition.
        /// </summary>
        public abstract void ApplyAfterSolving(Array u);

        /// <summary>
        /// This method sets the current time for time-dependent boundary conditions.
        /// </summary>
        public abstract void SetTime(Time t);
    }

    /// <summary>
    /// Neumann boundary condition (i.e., constant derivative)
    /// </summary>
    /// <remarks>
    /// Warning: The value passed must not be the value of the derivative.
    /// Instead, it must be comprehensive of the grid step between the first two points--i.e., 
    /// it must be the difference between f[0] and f[1].
    /// </remarks>
    public class NeumannBC : BoundaryCondition<TridiagonalOperator>
    {
        private readonly Real _value;
        private readonly Side _side;

        public NeumannBC(Real value, Side side)
        {
            _value = value;
            _side = side;
        }

        public override void ApplyBeforeApplying(TridiagonalOperator operatorL)
        {
            switch (_side)
            {
                case Side.Lower:
                    operatorL.SetFirstRow(-1.0, 1.0);
                    break;
                case Side.Upper:
                    operatorL.SetLastRow(-1.0, 1.0);
                    break;
                default:
                    QL.Fail("unknown side for Neumann boundary condition");
                    break;
            }
        }

        public override void ApplyAfterApplying(Array u)
        {
            switch (_side)
            {
                case Side.Lower:
                    u[0] = u[1] - _value;
                    break;
                case Side.Upper:
                    u[u.Count - 1] = u[u.Count - 2] + _value;
                    break;
                default:
                    QL.Fail("unknown side for Neumann boundary condition");
                    break;
            }
        }

        public override void ApplyBeforeSolving(TridiagonalOperator operatorL, Array rhs)
        {
            switch (_side)
            {
                case Side.Lower:
                    operatorL.SetFirstRow(-1.0, 1.0);
                    rhs[0] = _value;
                    break;
                case Side.Upper:
                    operatorL.SetLastRow(-1.0, 1.0);
                    rhs[rhs.Count - 1] = _value;
                    break;
                default:
                    QL.Fail("unknown side for Neumann boundary condition");
                    break;
            }
        }

        public override void ApplyAfterSolving(Array u)
        {
            // Empty implementation as in the original C++ code
        }

        public override void SetTime(Time t)
        {
            // Empty implementation for time-independent boundary conditions
        }
    }

    /// <summary>
    /// Dirichlet boundary condition (i.e., constant value)
    /// </summary>
    public class DirichletBC : BoundaryCondition<TridiagonalOperator>
    {
        private readonly Real _value;
        private readonly Side _side;

        public DirichletBC(Real value, Side side)
        {
            _value = value;
            _side = side;
        }

        public override void ApplyBeforeApplying(TridiagonalOperator operatorL)
        {
            switch (_side)
            {
                case Side.Lower:
                    operatorL.SetFirstRow(1.0, 0.0);
                    break;
                case Side.Upper:
                    operatorL.SetLastRow(0.0, 1.0);
                    break;
                default:
                    QL.Fail("unknown side for Dirichlet boundary condition");
                    break;
            }
        }

        public override void ApplyAfterApplying(Array u)
        {
            switch (_side)
            {
                case Side.Lower:
                    u[0] = _value;
                    break;
                case Side.Upper:
                    u[u.Count - 1] = _value;
                    break;
                default:
                    QL.Fail("unknown side for Dirichlet boundary condition");
                    break;
            }
        }

        public override void ApplyBeforeSolving(TridiagonalOperator operatorL, Array rhs)
        {
            switch (_side)
            {
                case Side.Lower:
                    operatorL.SetFirstRow(1.0, 0.0);
                    rhs[0] = _value;
                    break;
                case Side.Upper:
                    operatorL.SetLastRow(0.0, 1.0);
                    rhs[rhs.Count - 1] = _value;
                    break;
                default:
                    QL.Fail("unknown side for Dirichlet boundary condition");
                    break;
            }
        }

        public override void ApplyAfterSolving(Array u)
        {
            // Empty implementation as in the original C++ code
        }

        public override void SetTime(Time t)
        {
            // Empty implementation for time-independent boundary conditions
        }
    }
}