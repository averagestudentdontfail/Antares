// C# code for FdmBackwardSolver.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Finitedifferences;
using Antares.Method.Finitedifferences.Schemes;
using Antares.Method.Finitedifferences.Stepconditions;
using Antares.Method.Finitedifferences.Utilities;

namespace Antares.Method.Solver
{
    /// <summary>
    /// Backward-in-time solver for finite-difference models.
    /// </summary>
    public class FdmBackwardSolver
    {
        private readonly FdmLinearOpComposite _map;
        private readonly FdmBoundaryConditionSet _bcSet;
        private readonly FdmStepConditionComposite _condition;
        private readonly FdmSchemeDesc _schemeDesc;

        public FdmBackwardSolver(FdmLinearOpComposite map,
                                 FdmBoundaryConditionSet bcSet,
                                 FdmStepConditionComposite condition,
                                 FdmSchemeDesc schemeDesc)
        {
            _map = map;
            _bcSet = bcSet;
            _condition = condition ?? new FdmStepConditionComposite(new List<List<double>>(), new FdmStepConditionComposite.Conditions());
            _schemeDesc = schemeDesc;
        }

        public void Rollback(ref Array a, double from, double to, int steps, int dampingSteps)
        {
            double deltaT = from - to;
            int allSteps = steps + dampingSteps;
            double dampingTo = from - (deltaT * dampingSteps) / allSteps;

            if (dampingSteps > 0 && _schemeDesc.Type != FdmSchemeDesc.FdmSchemeType.ImplicitEulerType)
            {
                var implicitEvolver = new ImplicitEulerScheme(_map, _bcSet);
                var dampingModel = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, _condition.StoppingTimes());
                dampingModel.Rollback(ref a, from, dampingTo, dampingSteps, _condition);
            }

            switch (_schemeDesc.Type)
            {
                case FdmSchemeDesc.FdmSchemeType.HundsdorferType:
                {
                    var hsEvolver = new HundsdorferScheme(_schemeDesc.Theta, _schemeDesc.Mu, _map, _bcSet);
                    var hsModel = new FiniteDifferenceModel<HundsdorferScheme>(hsEvolver, _condition.StoppingTimes());
                    hsModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.DouglasType:
                {
                    var dsEvolver = new DouglasScheme(_schemeDesc.Theta, _map, _bcSet);
                    var dsModel = new FiniteDifferenceModel<DouglasScheme>(dsEvolver, _condition.StoppingTimes());
                    dsModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.CrankNicolsonType:
                {
                    var cnEvolver = new CrankNicolsonScheme(_schemeDesc.Theta, _map, _bcSet);
                    var cnModel = new FiniteDifferenceModel<CrankNicolsonScheme>(cnEvolver, _condition.StoppingTimes());
                    cnModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.CraigSneydType:
                {
                    var csEvolver = new CraigSneydScheme(_schemeDesc.Theta, _schemeDesc.Mu, _map, _bcSet);
                    var csModel = new FiniteDifferenceModel<CraigSneydScheme>(csEvolver, _condition.StoppingTimes());
                    csModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.ModifiedCraigSneydType:
                {
                    var mcsEvolver = new ModifiedCraigSneydScheme(_schemeDesc.Theta, _schemeDesc.Mu, _map, _bcSet);
                    var mcsModel = new FiniteDifferenceModel<ModifiedCraigSneydScheme>(mcsEvolver, _condition.StoppingTimes());
                    mcsModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.ImplicitEulerType:
                {
                    var implicitEvolver = new ImplicitEulerScheme(_map, _bcSet);
                    var implicitModel = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, _condition.StoppingTimes());
                    implicitModel.Rollback(ref a, from, to, allSteps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.ExplicitEulerType:
                {
                    var explicitEvolver = new ExplicitEulerScheme(_map, _bcSet);
                    var explicitModel = new FiniteDifferenceModel<ExplicitEulerScheme>(explicitEvolver, _condition.StoppingTimes());
                    explicitModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.MethodOfLinesType:
                {
                    var molEvolver = new MethodOfLinesScheme(_schemeDesc.Theta, _schemeDesc.Mu, _map, _bcSet);
                    var molModel = new FiniteDifferenceModel<MethodOfLinesScheme>(molEvolver, _condition.StoppingTimes());
                    molModel.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                case FdmSchemeDesc.FdmSchemeType.TrBDF2Type:
                {
                    FdmSchemeDesc trDesc = FdmSchemeDesc.CraigSneyd();
                    var csEvolver = new CraigSneydScheme(trDesc.Theta, trDesc.Mu, _map, _bcSet);
                    var trbdf2 = new TrBDF2Scheme( _schemeDesc.Theta, _map, csEvolver, _bcSet, _schemeDesc.Mu);
                    var trbdf2Model = new FiniteDifferenceModel<TrBDF2Scheme>(trbdf2, _condition.StoppingTimes());
                    trbdf2Model.Rollback(ref a, dampingTo, to, steps, _condition);
                    break;
                }
                default:
                    QL.Fail("Unknown scheme type");
                    break;
            }
        }
    }
}