# Class: QuantLib::step_iterator

## Brief Description
Iterator advancing in constant steps. 

## Detailed Description
This iterator advances an underlying random-access iterator in steps of 

## Types
### Typedef: iterator_category
- Type: typename std::iterator_traits< Iterator >::iterator_category

### Typedef: difference_type
- Type: typename std::iterator_traits< Iterator >::difference_type

### Typedef: value_type
- Type: typename std::iterator_traits< Iterator >::value_type

### Typedef: pointer
- Type: typename std::iterator_traits< Iterator >::pointer

### Typedef: reference
- Type: typename std::iterator_traits< Iterator >::reference

## Member Variables
- `private Iterator base_`: 
- `private None step_`: 

## Functions
### public None step_iterator()=default


### public None step_iterator(const Iterator &base, Size step)

#### Parameters:
- `const Iterator & base`: 
- `None step`: 

### public None step_iterator(const step_iterator< OtherIterator > &i, std::enable_if_t< std::is_convertible_v< OtherIterator, Iterator > > *=nullptr)

#### Parameters:
- `const  i`: 
- `std::enable_if_t< std::is_convertible_v< OtherIterator, Iterator > > * `: 

### public None step() const


### public None operator=(const step_iterator &other)=default

#### Parameters:
- `const  other`: 

### public None operator++()


### public None operator++(int)

#### Parameters:
- `int `: 

### public None operator*() const


### public None operator--()


### public None operator--(int)

#### Parameters:
- `int `: 

### public None operator+=(Size n)

#### Parameters:
- `None n`: 

### public None operator-=(Size n)

#### Parameters:
- `None n`: 

### public None operator[](Size n) const

#### Parameters:
- `None n`: 

