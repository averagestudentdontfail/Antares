# Class: QuantLib::BasketPayoff

## Inheritance
- Inherits from: QuantLib::Payoff

## Member Variables
- `private ext::shared_ptr<  basePayoff_`: 

## Functions
### public None BasketPayoff(ext::shared_ptr< Payoff > p)

#### Parameters:
- `ext::shared_ptr<  p`: 

### public None ~BasketPayoff() override=default


### public std::string name() const override


### public std::string description() const override


### public None operator()(Real price) const override

#### Parameters:
- `None price`: 

### public None operator()(const Array &a) const

#### Parameters:
- `const  a`: 

### public None accumulate(const Array &a) const =0

#### Parameters:
- `const  a`: 

### public ext::shared_ptr<  basePayoff()


