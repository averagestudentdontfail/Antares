# Class: QuantLib::FdmLinearOpIterator

## Member Variables
- `private None index_`: 
- `private std::vector<  dim_`: 
- `private std::vector<  coordinates_`: 

## Functions
### public None FdmLinearOpIterator(Size index=0)

#### Parameters:
- `None index`: 

### public None FdmLinearOpIterator(std::vector< Size > dim)

#### Parameters:
- `std::vector<  dim`: 

### public None FdmLinearOpIterator(std::vector< Size > dim, std::vector< Size > coordinates, Size index)

#### Parameters:
- `std::vector<  dim`: 
- `std::vector<  coordinates`: 
- `None index`: 

### public void operator++()


### public const  operator*() const


### public bool operator!=(const FdmLinearOpIterator &iterator) const

#### Parameters:
- `const  iterator`: 

### public None index() const


### public const std::vector<  coordinates() const


### public void swap(FdmLinearOpIterator &iter) noexcept

#### Parameters:
- `None iter`: 

