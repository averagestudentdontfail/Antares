# Class: QuantLib::BlackCalculator

## Brief Description
Black 1976 calculator class. 

## Member Variables
- `protected None strike_`: 
- `protected None forward_`: 
- `protected None stdDev_`: 
- `protected None discount_`: 
- `protected None variance_`: 
- `protected None d1_`: 
- `protected None d2_`: 
- `protected None alpha_`: 
- `protected None beta_`: 
- `protected None DalphaDd1_`: 
- `protected None DbetaDd2_`: 
- `protected None n_d1_`: 
- `protected None cum_d1_`: 
- `protected None n_d2_`: 
- `protected None cum_d2_`: 
- `protected None x_`: 
- `protected None DxDs_`: 
- `protected None DxDstrike_`: 

## Functions
### public None BlackCalculator(const ext::shared_ptr< StrikedTypePayoff > &payoff, Real forward, Real stdDev, Real discount=1.0)

#### Parameters:
- `const ext::shared_ptr<  payoff`: 
- `None forward`: 
- `None stdDev`: 
- `None discount`: 

### public None BlackCalculator(Option::Type optionType, Real strike, Real forward, Real stdDev, Real discount=1.0)

#### Parameters:
- `None optionType`: 
- `None strike`: 
- `None forward`: 
- `None stdDev`: 
- `None discount`: 

### public None ~BlackCalculator()=default


### public None value() const


### public None deltaForward() const


### public None delta(Real spot) const

#### Parameters:
- `None spot`: 

### public None elasticityForward() const


### public None elasticity(Real spot) const

#### Parameters:
- `None spot`: 

### public None gammaForward() const


### public None gamma(Real spot) const

#### Parameters:
- `None spot`: 

### public None theta(Real spot, Time maturity) const

#### Parameters:
- `None spot`: 
- `None maturity`: 

### public None thetaPerDay(Real spot, Time maturity) const

#### Parameters:
- `None spot`: 
- `None maturity`: 

### public None vega(Time maturity) const

#### Parameters:
- `None maturity`: 

### public None rho(Time maturity) const

#### Parameters:
- `None maturity`: 

### public None dividendRho(Time maturity) const

#### Parameters:
- `None maturity`: 

### public None itmCashProbability() const


### public None itmAssetProbability() const


### public None strikeSensitivity() const


### public None strikeGamma() const


### public None alpha() const


### public None beta() const


## Functions
### protected void initialize(const ext::shared_ptr< StrikedTypePayoff > &p)

#### Parameters:
- `const ext::shared_ptr<  p`: 

