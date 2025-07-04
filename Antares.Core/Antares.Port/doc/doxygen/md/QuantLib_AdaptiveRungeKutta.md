# Class: QuantLib::AdaptiveRungeKutta

## Types
### Typedef: OdeFct
- Type: std::function< std::vector< T >(const 

### Typedef: OdeFct1d
- Type: std::function< T(const 

## Member Variables
- `private const std::vector< T > yStart_`: 
- `private const  eps_`: 
- `private const  h1_`: 
- `private const  hmin_`: 
- `private const  a2`: 
- `private const  a3`: 
- `private const  a4`: 
- `private const  a5`: 
- `private const  a6`: 
- `private const  b21`: 
- `private const  b31`: 
- `private const  b32`: 
- `private const  b41`: 
- `private const  b42`: 
- `private const  b43`: 
- `private const  b51`: 
- `private const  b52`: 
- `private const  b53`: 
- `private const  b54`: 
- `private const  b61`: 
- `private const  b62`: 
- `private const  b63`: 
- `private const  b64`: 
- `private const  b65`: 
- `private const  c1`: 
- `private const  c3`: 
- `private const  c4`: 
- `private const  c6`: 
- `private const  dc1`: 
- `private const  dc3`: 
- `private const  dc4`: 
- `private const  dc5`: 
- `private const  dc6`: 
- `private const double ADAPTIVERK_MAXSTP`: 
- `private const double ADAPTIVERK_TINY`: 
- `private const double ADAPTIVERK_SAFETY`: 
- `private const double ADAPTIVERK_PGROW`: 
- `private const double ADAPTIVERK_PSHRINK`: 
- `private const double ADAPTIVERK_ERRCON`: 

## Functions
### public None AdaptiveRungeKutta(const Real eps=1.0e-6, const Real h1=1.0e-4, const Real hmin=0.0)

#### Parameters:
- `const  eps`: 
- `const  h1`: 
- `const  hmin`: 

### public std::vector< T > operator()(const OdeFct &ode, const std::vector< T > &y1, Real x1, Real x2)

#### Parameters:
- `const  ode`: 
- `const std::vector< T > & y1`: 
- `None x1`: 
- `None x2`: 

### public T operator()(const OdeFct1d &ode, T y1, Real x1, Real x2)

#### Parameters:
- `const  ode`: 
- `T y1`: 
- `None x1`: 
- `None x2`: 

## Functions
### private void rkqs(std::vector< T > &y, const std::vector< T > &dydx, Real &x, Real htry, Real eps, const std::vector< Real > &yScale, Real &hdid, Real &hnext, const OdeFct &derivs)

#### Parameters:
- `std::vector< T > & y`: 
- `const std::vector< T > & dydx`: 
- `None x`: 
- `None htry`: 
- `None eps`: 
- `const std::vector<  yScale`: 
- `None hdid`: 
- `None hnext`: 
- `const  derivs`: 

### private void rkck(const std::vector< T > &y, const std::vector< T > &dydx, Real x, Real h, std::vector< T > &yout, std::vector< T > &yerr, const OdeFct &derivs)

#### Parameters:
- `const std::vector< T > & y`: 
- `const std::vector< T > & dydx`: 
- `None x`: 
- `None h`: 
- `std::vector< T > & yout`: 
- `std::vector< T > & yerr`: 
- `const  derivs`: 

