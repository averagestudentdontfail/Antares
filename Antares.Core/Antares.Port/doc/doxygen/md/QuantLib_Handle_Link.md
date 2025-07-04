# Class: QuantLib::Handle::Link

## Inheritance
- Inherits from: QuantLib::Observable
- Inherits from: QuantLib::Observer

## Member Variables
- `private ext::shared_ptr< T > h_`: 
- `private bool isObserver_`: 

## Functions
### public None Link(const ext::shared_ptr< T > &h, bool registerAsObserver)

#### Parameters:
- `const ext::shared_ptr< T > & h`: 
- `bool registerAsObserver`: 

### public None Link(ext::shared_ptr< T > &&h, bool registerAsObserver)

#### Parameters:
- `ext::shared_ptr< T > && h`: 
- `bool registerAsObserver`: 

### public void linkTo(ext::shared_ptr< T >, bool registerAsObserver)

#### Parameters:
- `ext::shared_ptr< T > `: 
- `bool registerAsObserver`: 

### public bool empty() const


### public const ext::shared_ptr< T > & currentLink() const


### public void update() override


