# Class: QuantLib::FdmMesherComposite

## Inheritance
- Inherits from: QuantLib::FdmMesher

## Member Variables
- `private const std::vector< ext::shared_ptr<  mesher_`: 

## Functions
### public None FdmMesherComposite(const ext::shared_ptr< FdmLinearOpLayout > &layout, const std::vector< ext::shared_ptr< Fdm1dMesher > > &mesher)

#### Parameters:
- `const ext::shared_ptr<  layout`: 
- `const std::vector< ext::shared_ptr<  mesher`: 

### public None FdmMesherComposite(const std::vector< ext::shared_ptr< Fdm1dMesher > > &mesher)

#### Parameters:
- `const std::vector< ext::shared_ptr<  mesher`: 

### public None FdmMesherComposite(const ext::shared_ptr< Fdm1dMesher > &mesher)

#### Parameters:
- `const ext::shared_ptr<  mesher`: 

### public None FdmMesherComposite(const ext::shared_ptr< Fdm1dMesher > &m1, const ext::shared_ptr< Fdm1dMesher > &m2)

#### Parameters:
- `const ext::shared_ptr<  m1`: 
- `const ext::shared_ptr<  m2`: 

### public None FdmMesherComposite(const ext::shared_ptr< Fdm1dMesher > &m1, const ext::shared_ptr< Fdm1dMesher > &m2, const ext::shared_ptr< Fdm1dMesher > &m3)

#### Parameters:
- `const ext::shared_ptr<  m1`: 
- `const ext::shared_ptr<  m2`: 
- `const ext::shared_ptr<  m3`: 

### public None FdmMesherComposite(const ext::shared_ptr< Fdm1dMesher > &m1, const ext::shared_ptr< Fdm1dMesher > &m2, const ext::shared_ptr< Fdm1dMesher > &m3, const ext::shared_ptr< Fdm1dMesher > &m4)

#### Parameters:
- `const ext::shared_ptr<  m1`: 
- `const ext::shared_ptr<  m2`: 
- `const ext::shared_ptr<  m3`: 
- `const ext::shared_ptr<  m4`: 

### public None dplus(const FdmLinearOpIterator &iter, Size direction) const override

#### Parameters:
- `const  iter`: 
- `None direction`: 

### public None dminus(const FdmLinearOpIterator &iter, Size direction) const override

#### Parameters:
- `const  iter`: 
- `None direction`: 

### public None location(const FdmLinearOpIterator &iter, Size direction) const override

#### Parameters:
- `const  iter`: 
- `None direction`: 

### public None locations(Size direction) const override

#### Parameters:
- `None direction`: 

### public const std::vector< ext::shared_ptr<  getFdm1dMeshers() const


