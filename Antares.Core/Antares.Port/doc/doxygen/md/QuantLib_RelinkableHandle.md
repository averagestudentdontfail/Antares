# Class: QuantLib::RelinkableHandle

## Brief Description
Relinkable handle to an observable. 

## Detailed Description
An instance of this class can be relinked so that it points to another observable. The change will be propagated to all handles that were created as copies of such instance.

## Inheritance
- Inherits from: QuantLib::Handle< T >

## Functions
### public None RelinkableHandle()


### public None RelinkableHandle(const ext::shared_ptr< T > &p, bool registerAsObserver=true)

#### Parameters:
- `const ext::shared_ptr< T > & p`: 
- `bool registerAsObserver`: 

### public None RelinkableHandle(ext::shared_ptr< T > &&p, bool registerAsObserver=true)

#### Parameters:
- `ext::shared_ptr< T > && p`: 
- `bool registerAsObserver`: 

### public None RelinkableHandle(T *p, bool registerAsObserver=true)

#### Parameters:
- `T * p`: 
- `bool registerAsObserver`: 

### public void linkTo(const ext::shared_ptr< T > &h, bool registerAsObserver=true)

#### Parameters:
- `const ext::shared_ptr< T > & h`: 
- `bool registerAsObserver`: 

### public void linkTo(ext::shared_ptr< T > &&h, bool registerAsObserver=true)

#### Parameters:
- `ext::shared_ptr< T > && h`: 
- `bool registerAsObserver`: 

### public void reset()


