# Class: QuantLib::QdPlusBoundaryEvaluator

## Member Variables
- `private const  Phi`: 
- `private const  phi`: 
- `private const  tau`: 
- `private const  K`: 
- `private const  sigma`: 
- `private const  sigma2`: 
- `private const  v`: 
- `private const  r`: 
- `private const  q`: 
- `private const  dr`: 
- `private const  dq`: 
- `private const  ddr`: 
- `private const  omega`: 
- `private const  lambda`: 
- `private const  lambdaPrime`: 
- `private const  alpha`: 
- `private const  beta`: 
- `private const  xMax`: 
- `private const  xMin`: 
- `private None nrEvaluations`: 
- `private None sc`: 
- `private None dp`: 
- `private None dm`: 
- `private None Phi_dp`: 
- `private None Phi_dm`: 
- `private None phi_dp`: 
- `private None npv`: 
- `private None theta`: 
- `private None charm`: 

## Functions
### public None QdPlusBoundaryEvaluator(Real S, Real strike, Rate rf, Rate dy, Volatility vol, Time t, Time T)

#### Parameters:
- `None S`: 
- `None strike`: 
- `None rf`: 
- `None dy`: 
- `None vol`: 
- `None t`: 
- `None T`: 

### public None operator()(Real S) const

#### Parameters:
- `None S`: 

### public None derivative(Real S) const

#### Parameters:
- `None S`: 

### public None fprime2(Real S) const

#### Parameters:
- `None S`: 

### public None xmin() const


### public None xmax() const


### public None evaluations() const


## Functions
### private void preCalculate(Real S) const

#### Parameters:
- `None S`: 

