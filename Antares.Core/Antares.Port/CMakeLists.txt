
cmake_minimum_required(VERSION 3.10)
project(QuantLibAmericanEngine CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Add the main QuantLib include directory
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/ql
)

# List all source files
set(ENGINE_SOURCES
    ql/cashflow.cpp
    ql/cashflows/dividend.cpp
    ql/errors.cpp
    ql/event.cpp
    ql/exercise.cpp
    ql/instrument.cpp
    ql/instruments/basketoption.cpp
    ql/instruments/impliedvolatility.cpp
    ql/instruments/multiassetoption.cpp
    ql/instruments/oneassetoption.cpp
    ql/instruments/payoffs.cpp
    ql/instruments/vanillaoption.cpp
    ql/interestrate.cpp
    ql/math/distributions/gammadistribution.cpp
    ql/math/distributions/normaldistribution.cpp
    ql/math/errorfunction.cpp
    ql/math/integrals/gaussianorthogonalpolynomial.cpp
    ql/math/integrals/gaussianquadratures.cpp
    ql/math/integrals/gausslobattointegral.cpp
    ql/math/integrals/integral.cpp
    ql/math/interpolations/chebyshevinterpolation.cpp
    ql/math/matrix.cpp
    ql/math/matrixutilities/bicgstab.cpp
    ql/math/matrixutilities/gmres.cpp
    ql/math/matrixutilities/qrdecomposition.cpp
    ql/math/matrixutilities/symmetricschurdecomposition.cpp
    ql/math/matrixutilities/tqreigendecomposition.cpp
    ql/math/optimization/lmdif.cpp
    ql/methods/finitedifferences/boundarycondition.cpp
    ql/methods/finitedifferences/meshers/concentrating1dmesher.cpp
    ql/methods/finitedifferences/meshers/fdmblackscholesmesher.cpp
    ql/methods/finitedifferences/meshers/fdmmeshercomposite.cpp
    ql/methods/finitedifferences/operators/fdmblackscholesop.cpp
    ql/methods/finitedifferences/operators/fdmlinearoplayout.cpp
    ql/methods/finitedifferences/operators/firstderivativeop.cpp
    ql/methods/finitedifferences/operators/secondderivativeop.cpp
    ql/methods/finitedifferences/operators/triplebandlinearop.cpp
    ql/methods/finitedifferences/schemes/craigsneydscheme.cpp
    ql/methods/finitedifferences/schemes/cranknicolsonscheme.cpp
    ql/methods/finitedifferences/schemes/douglasscheme.cpp
    ql/methods/finitedifferences/schemes/expliciteulerscheme.cpp
    ql/methods/finitedifferences/schemes/hundsdorferscheme.cpp
    ql/methods/finitedifferences/schemes/impliciteulerscheme.cpp
    ql/methods/finitedifferences/schemes/methodoflinesscheme.cpp
    ql/methods/finitedifferences/schemes/modifiedcraigsneydscheme.cpp
    ql/methods/finitedifferences/solvers/fdm1dimsolver.cpp
    ql/methods/finitedifferences/solvers/fdmbackwardsolver.cpp
    ql/methods/finitedifferences/solvers/fdmblackscholessolver.cpp
    ql/methods/finitedifferences/stepconditions/fdmamericanstepcondition.cpp
    ql/methods/finitedifferences/stepconditions/fdmbermudanstepcondition.cpp
    ql/methods/finitedifferences/stepconditions/fdmsnapshotcondition.cpp
    ql/methods/finitedifferences/stepconditions/fdmstepconditioncomposite.cpp
    ql/methods/finitedifferences/tridiagonaloperator.cpp
    ql/methods/finitedifferences/utilities/escroweddividendadjustment.cpp
    ql/methods/finitedifferences/utilities/fdmdividendhandler.cpp
    ql/methods/finitedifferences/utilities/fdmescrowedloginnervaluecalculator.cpp
    ql/methods/finitedifferences/utilities/fdminnervaluecalculator.cpp
    ql/methods/finitedifferences/utilities/fdmquantohelper.cpp
    ql/patterns/observable.cpp
    ql/pricingengines/blackcalculator.cpp
    ql/pricingengines/vanilla/analyticdividendeuropeanengine.cpp
    ql/pricingengines/vanilla/analyticeuropeanengine.cpp
    ql/pricingengines/vanilla/fdblackscholesvanillaengine.cpp
    ql/pricingengines/vanilla/qdfpamericanengine.cpp
    ql/pricingengines/vanilla/qdplusamericanengine.cpp
    ql/processes/blackscholesprocess.cpp
    ql/processes/eulerdiscretization.cpp
    ql/quote.cpp
    ql/settings.cpp
    ql/stochasticprocess.cpp
    ql/termstructure.cpp
    ql/termstructures/volatility/equityfx/blackvariancecurve.cpp
    ql/termstructures/volatility/equityfx/blackvoltermstructure.cpp
    ql/termstructures/volatility/equityfx/localvolsurface.cpp
    ql/termstructures/volatility/equityfx/localvoltermstructure.cpp
    ql/termstructures/voltermstructure.cpp
    ql/termstructures/yield/flatforward.cpp
    ql/termstructures/yield/zeroyieldstructure.cpp
    ql/termstructures/yieldtermstructure.cpp
    ql/time/businessdayconvention.cpp
    ql/time/calendar.cpp
    ql/time/date.cpp
    ql/time/daycounters/actual365fixed.cpp
    ql/time/frequency.cpp
    ql/time/period.cpp
    ql/time/timeunit.cpp
    ql/time/weekday.cpp
    ql/timegrid.cpp
    ql/utilities/dataformatters.cpp
)

add_library(quantlib_american_engine STATIC ${ENGINE_SOURCES})
