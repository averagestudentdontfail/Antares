// C# code for OperatorTraits.cs

using System.Collections.Generic;

namespace Antares.Method
{
    /// <summary>
    /// Differential operator traits
    /// </summary>
    /// <typeparam name="TOperator">The operator type</typeparam>
    /// <typeparam name="TArray">The array type used by the operator</typeparam>
    public class OperatorTraits<TOperator, TArray>
    {
        /// <summary>
        /// The operator type
        /// </summary>
        public class OperatorType 
        {
            public static implicit operator System.Type(OperatorType _) => typeof(TOperator);
        }

        /// <summary>
        /// The array type used by the operator
        /// </summary>
        public class ArrayType 
        {
            public static implicit operator System.Type(ArrayType _) => typeof(TArray);
        }

        /// <summary>
        /// The boundary condition type for this operator
        /// </summary>
        public class BcType 
        {
            public static implicit operator System.Type(BcType _) => typeof(BoundaryCondition<TOperator>);
        }

        /// <summary>
        /// The boundary condition set type (collection of boundary conditions)
        /// </summary>
        public class BcSet 
        {
            public static implicit operator System.Type(BcSet _) => typeof(List<BoundaryCondition<TOperator>>);
        }

        /// <summary>
        /// The step condition type for this array type
        /// </summary>
        public class ConditionType 
        {
            public static implicit operator System.Type(ConditionType _) => typeof(IStepCondition<TArray>);
        }
    }

    /// <summary>
    /// Specialized operator traits for TridiagonalOperator with Array
    /// </summary>
    public class TridiagonalOperatorTraits : OperatorTraits<TridiagonalOperator, Array>
    {
        /// <summary>
        /// Type alias for the operator
        /// </summary>
        public static readonly System.Type Operator = typeof(TridiagonalOperator);

        /// <summary>
        /// Type alias for the array
        /// </summary>
        public static readonly System.Type Array = typeof(Array);

        /// <summary>
        /// Type alias for boundary condition
        /// </summary>
        public static readonly System.Type BoundaryCondition = typeof(BoundaryCondition<TridiagonalOperator>);

        /// <summary>
        /// Type alias for boundary condition set
        /// </summary>
        public static readonly System.Type BoundaryConditionSet = typeof(List<BoundaryCondition<TridiagonalOperator>>);

        /// <summary>
        /// Type alias for step condition
        /// </summary>
        public static readonly System.Type StepCondition = typeof(IStepCondition<Array>);
    }
}