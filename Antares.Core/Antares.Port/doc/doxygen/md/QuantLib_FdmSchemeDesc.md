# Struct: QuantLib::FdmSchemeDesc

## Types
### Enum: FdmSchemeType
- HundsdorferType
- DouglasType
- CraigSneydType
- ModifiedCraigSneydType
- ImplicitEulerType
- ExplicitEulerType
- MethodOfLinesType
- TrBDF2Type
- CrankNicolsonType

## Member Variables
- `public const  type`: 
- `public const  theta`: 
- `public const  mu`: 

## Functions
### public None FdmSchemeDesc(FdmSchemeType type, Real theta, Real mu)

#### Parameters:
- `None type`: 
- `None theta`: 
- `None mu`: 

## Functions
### static public None Douglas()


### static public None CrankNicolson()


### static public None ImplicitEuler()


### static public None ExplicitEuler()


### static public None CraigSneyd()


### static public None ModifiedCraigSneyd()


### static public None Hundsdorfer()


### static public None ModifiedHundsdorfer()


### static public None MethodOfLines(Real eps=0.001, Real relInitStepSize=0.01)

#### Parameters:
- `None eps`: 
- `None relInitStepSize`: 

### static public None TrBDF2()


