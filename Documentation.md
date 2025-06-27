# Anderson Derivative Pricing Engine: A Unified Spectral Method for Single and Double Boundary American Options

## Abstract

This paper presents a comprehensive mathematical framework for a high-performance derivative pricing engine based on the spectral collocation methodology of Andersen, Lake, and Offengenden. The architecture is designed for pricing American-style options, unifying the treatment of standard single exercise boundary cases with the more complex double-boundary scenarios that arise in negative interest rate environments. By leveraging sophisticated fixed-point iteration schemes, advanced integral equation formulations, and spectral numerical methods, the framework achieves exceptional accuracy and computational speed. The methodology transforms the free-boundary problem of American option pricing into a system of integral equations for the optimal exercise boundary, which are solved with exponential convergence rates. For double-boundary cases, the framework employs a novel decoupled iteration system, allowing each boundary to be computed independently, thereby preserving the method's efficiency and accuracy. This paper provides a complete theoretical and mathematical blueprint for the engine, detailing the foundational equations, the numerical techniques for both single and double-boundary topologies, and the mathematical optimizations that ensure high performance.

## 1. Introduction

The valuation of American options presents a significant challenge in computational finance, primarily due to the early exercise feature which introduces a free boundary into the pricing problem. The core task is the determination of the optimal exercise boundary, a time-dependent asset price threshold that divides the state space into a continuation region and an exercise region. While traditional numerical techniques such as binomial trees and finite difference methods are widely used, they typically exhibit algebraic convergence rates, requiring substantial computational resources to achieve high precision .

This paper details the mathematical architecture of a pricing engine that overcomes these limitations by employing the spectral collocation method introduced by Andersen, Lake, and Offengenden . This approach recasts the American option pricing problem as the solution to a non-linear integral equation for the exercise boundary, which is then solved using a fixed-point iteration scheme. The use of Chebyshev polynomial interpolation on a transformed boundary, combined with high-order numerical quadrature, leads to spectral (i.e., exponential) convergence rates, enabling computational throughput of over 100,000 option prices per second with accuracy often exceeding 10 significant digits .

A significant challenge in modern financial markets is the presence of negative interest rates, a situation explicitly excluded in many classical pricing models . Negative rates can lead to a more complex exercise topology, where the exercise region is bounded by two distinct functions of time, creating a "double-boundary" case . This paper extends the foundational spectral method to robustly and efficiently handle these scenarios. Drawing on the work of Andersen and Lake, we present a complete mathematical recipe that characterizes the topology of the double-boundary exercise region, provides its short- and long-term asymptotics, and develops a decoupled fixed-point iteration system that allows each boundary to be calculated with high precision independently of the other .

The resulting framework provides a unified and robust mathematical architecture for pricing American options across all possible sign combinations of interest rates and dividend yields, making it an invaluable tool for applications requiring both real-time performance and high fidelity, such as the risk management of large derivatives portfolios .

## 2. Mathematical Framework

### 2.1 Process Dynamics

We assume the underlying asset price, $S(t)$, follows a geometric Brownian motion under the risk-neutral measure $\mathbb{Q}$, governed by the stochastic differential equation (SDE):

$$\frac{dS(t)}{S(t)} = (r-q)dt + \sigma dW(t)$$ 

where:
* $S(t)$ is the asset price at time $t$.
* $r$ is the constant risk-free interest rate .
* $q$ is the constant continuous dividend yield . For options on futures, $q=r$, implying a drift of $\mu=0$ .
* $\sigma$ is the constant asset price volatility .
* $W(t)$ is a standard Wiener process under $\mathbb{Q}$ .

Because the model parameters are constant, the process is time-homogeneous, and the option price at time $t$ for a contract maturing at $T$ can be expressed as a function of the time to maturity, $\tau \triangleq T-t$, and the current asset price, $S(t) = S$ .

### 2.2 The American Option Problem

We focus on the American put option with strike price $K$ and maturity $T$. Its value at time $t$ is the solution to an optimal stopping problem:

$$V(\tau, S) = \sup_{\nu \in [t, T]} \mathbb{E}_t^\mathbb{Q} \left[ e^{-r(\nu - t)} (K - S(\nu))^+ \right]$$ 

where the supremum is taken over all stopping times $\nu$ between $t$ and $T$. The optimal exercise strategy is characterized by a time-dependent, deterministic exercise boundary, $B(\tau)$, such that it is optimal to exercise the put option if the asset price falls to or below this boundary .

### 2.3 Integral Equation Representation (Single Boundary)

A cornerstone of the spectral method is the representation of the American option price as the sum of its European equivalent and an early exercise premium. For a single, continuous exercise boundary $B(u)$, the price of an American put option $V(\tau, S)$ is given by the integral equation:

$$V(\tau,S) = v(\tau,S) + \int_{0}^{\tau} rKe^{-r(\tau-u)}\Phi(-d_{-}(\tau-u,S/B(u)))du - \int_{0}^{\tau} qSe^{-q(\tau-u)}\Phi(-d_{+}(\tau-u,S/B(u)))du$$ 

where:
* $v(\tau, S)$ is the price of the corresponding European put option.
* $\Phi(\cdot)$ is the standard normal cumulative distribution function.
* $d_{\pm}(\tau, z) \triangleq \frac{\ln(z) + (r-q)\tau \pm \frac{1}{2}\sigma^2\tau}{\sigma\sqrt{\tau}}$ .

The integral terms represent the present value of the "carry" associated with the early exercise right . This cash flow stream, $(rK - qS(s))ds$, arises from the interest earned on the strike price $K$ and the dividend payments forgone on the short stock position that would be established upon exercise .

## 3. The Optimal Exercise Boundary (Single Boundary Case)

To utilize the integral representation, the exercise boundary function $B(\tau)$ must be determined. This is achieved by solving a non-linear integral equation derived from the boundary conditions of the American option problem.

### 3.1 Boundary Conditions

At the exercise boundary $S=B(\tau)$, the American option price must satisfy two critical conditions:
1.  **Value Matching**: The option's value must equal its intrinsic value.
    $$V(\tau, B(\tau)) = K - B(\tau)$$ 
2.  **Smooth Pasting**: The option's delta must be continuous and equal to -1 for a put.
    $$V_S(\tau, B(\tau)) = -1$$ 

Combining these conditions with the integral price formula (and its derivatives) yields various integral equations for the boundary $B(\tau)$ .

### 3.2 Asymptotic Behavior

The shape of $B(\tau)$ is characterized by its behavior at short and long maturities.

* **Short-Maturity ($\tau \to 0^+$)**: The boundary approaches a limit $X$ that depends on the sign of $r-q$.
    $$\lim_{\tau \to 0^+} B(\tau) = X = \begin{cases} K & \text{if } r \ge q \\ K(r/q) & \text{if } r < q \end{cases}$$ 
    The boundary is generally smooth for $\tau > 0$ but can be discontinuous at $\tau=0$ if $r<q$ . Its derivatives are unbounded at the origin .

* **Long-Maturity ($\tau \to \infty$)**: The boundary asymptotically approaches a constant level, $B_\infty$, corresponding to the perpetual American option boundary.
    $$B_\infty = K \frac{\theta_-}{\theta_- - 1} \quad \text{where} \quad \theta_- = \frac{-(\frac{r-q}{\sigma^2} - \frac{1}{2}) - \sqrt{(\frac{r-q}{\sigma^2} - \frac{1}{2})^2 + \frac{2r}{\sigma^2}}}{2}$$ 

### 3.3 Fixed-Point Iteration Systems

The integral equations for the boundary can be rearranged into a fixed-point form suitable for iterative solution:

$$B(\tau) = K e^{-(r-q)\tau} \frac{N(\tau, B)}{D(\tau, B)}$$ 

where $N$ and $D$ are functionals of the entire boundary function $B(u)$ for $u \le \tau$. Two primary systems are derived from the boundary conditions.

* **System B (FP-B) - Value Matching Formulation**: Derived from the value matching condition, this system is given by:
    $$N_B(\tau,B)=\Phi(d_{-}(\tau,B(\tau)/K))+r\int_{0}^{\tau}e^{ru}\Phi(d_{-}(\tau-u,B(\tau)/B(u)))du$$ 
    $$D_B(\tau,B)=\Phi(d_{+}(\tau,B(\tau)/K))+q\int_{0}^{\tau}e^{qu}\Phi(d_{+}(\tau-u,B(\tau)/B(u)))du$$ 

* **System A (FP-A) - Smooth Pasting Formulation**: Derived from the smooth pasting condition, this system is more complex but often exhibits superior convergence properties . The functionals are:
    $$N_A(\tau,B)=\frac{\phi(d_{-}(\tau,B(\tau)/K))}{\sigma\sqrt{\tau}}+r\mathcal{K}_{3}(\tau)$$ 
    $$D_A(\tau,B)=\frac{\phi(d_{+}(\tau,B(\tau)/K))}{\sigma\sqrt{\tau}}+\Phi(d_{+}(\tau,B(\tau)/K))+q(\mathcal{K}_{1}(\tau)+\mathcal{K}_{2}(\tau))$$ 
    where $\phi(\cdot)$ is the standard normal probability density function and the integral operators are:
    $$\mathcal{K}_{1}(\tau)=\int_{0}^{\tau}e^{qu}\Phi(d_{+}(\tau-u,B(\tau)/B(u)))du$$ 
    $$\mathcal{K}_{2}(\tau)=\int_{0}^{\tau}\frac{e^{qu}}{\sigma\sqrt{\tau-u}}\phi(d_{+}(\tau-u,B(\tau)/B(u)))du$$ 
    $$\mathcal{K}_{3}(\tau)=\int_{0}^{\tau}\frac{e^{ru}}{\sigma\sqrt{\tau-u}}\phi(d_{-}(\tau-u,B(\tau)/B(u)))du$$ 

## 4. The Double-Boundary Case (Negative Rates)

When interest rates and dividend yields are negative, the topology of the exercise region can change dramatically, leading to two exercise boundaries .

### 4.1 Conditions for Double Boundaries

For an American put option, a double-boundary exercise region arises when both the risk-free rate and dividend yield are negative, with the dividend yield being more negative than the rate:

* **American Put**: A double boundary occurs if $q < r < 0$ . The exercise region becomes the finite interval $(Kr/q, K)$ just prior to maturity .
* **American Call**: By put-call symmetry, a double boundary occurs if $r < q < 0$ . The exercise region becomes $(K, Kr/q)$ just prior to maturity .

In these cases, there exists both a standard exercise boundary (denoted $B(\tau)$) and a second boundary (denoted $Y(\tau)$) beyond which it is no longer optimal to hold the option, creating a finite exercise interval $[Y(\tau), B(\tau)]$ for a put, or $[B(\tau), Y(\tau)]$ for a call .

### 4.2 Topology and Asymptotics

The behavior of the two boundaries is characterized by their short- and long-maturity limits and a critical volatility $\sigma^*$.

* **Short-Maturity Asymptotics ($\tau \to 0^+$)**: The two boundaries start at:
    * **Put ($q<r<0$)**: $B_0 = K$ and $Y_0 = Kr/q$ .
    * **Call ($r<q<0$)**: $B_0 = K$ and $Y_0 = Kr/q$ .

* **Long-Maturity Asymptotics ($\tau \to \infty$)**: The behavior depends on the relationship between the option's volatility $\sigma$ and a critical volatility $\sigma^* = |\sqrt{-2r} - \sqrt{-2q}|$ .
    1.  If $\sigma > \sigma^*$: The boundaries do not have finite long-term limits. They intersect at a finite time $\tau^*$, "pinching off" the exercise region. For maturities $\tau > \tau^*$, early exercise is never optimal .
    2.  If $\sigma < \sigma^*$: The boundaries never intersect and converge to distinct finite limits $B_\infty$ and $Y_\infty$, which have closed-form expressions derived from perpetual American option theory .
    3.  If $\sigma = \sigma^*$: The boundaries converge to the same value at infinity: $B_\infty = Y_\infty = K\sqrt{r/q}$ .

### 4.3 Integral Equation Representation (Double Boundary)

With a double-boundary exercise region $\mathcal{A}(s)=[Y(T-s), B(T-s)]$, the integral equation for the American put price is extended. The price is the European price plus the present value of the early exercise premium, which is now integrated over the finite exercise interval.

**Corollary 1 (Andersen & Lake, 2021)**: For an American put where $q < r < 0$, the value $V$ is given by:

$$V(\tau,S,K) = p(\tau,S,K) + rK\int_{0}^{\min(\tau,\tau^{*})}[p_{K}(\tau-u,S,B(u))-p_{K}(\tau-u,S,Y(u))]du \\ + qS\int_{0}^{\min(\tau,\tau^{*})}[p_{S}(\tau-u,S,B(u))-p_{S}(\tau-u,S,Y(u))]du$$ 

where $p_S$ and $p_K$ are the partial derivatives of the European put price with respect to the spot price and strike, respectively .

### 4.4 Decoupled Fixed-Point Iteration Systems

A key innovation for the double-boundary case is that the integral representation can be manipulated to yield two *decoupled* equations for the boundaries $B$ and $Y$, allowing them to be solved separately . This avoids a more complex and less accurate simultaneous iteration .

This is enabled by **Corollary 2 (Andersen & Lake, 2021)**, which shows that for an asset price $S$ outside the exercise interval, the price depends only on the boundary that would be hit first .

* **Fixed-Point System for B (FP-B)**: By applying the value-matching condition at the upper boundary $S=B(\tau)$, we obtain a fixed-point system for $B$ that does not depend on $Y$:
    $$N_{B}(\tau,B) = -c_{K}(\tau,B(\tau),K) - r\int_{0}^{\tau}c_{K}(\tau-u,B(\tau),B(u))du$$ 
    $$D_{B}(\tau,B) = c_{S}(\tau,B(\tau),K) + q\int_{0}^{\tau}c_{S}(\tau-u,B(\tau),B(u))du$$ 
    where $c_S$ and $c_K$ are derivatives of the European call price. This system has the same form as the single-boundary case .

* **Fixed-Point System for Y (FP-Y)**: By applying the smooth-pasting condition at the lower boundary $S=Y(\tau)$, we obtain a fixed-point system for $Y$ that does not depend on $B$:
    $$N_{Y}(\tau,Y) = rK\int_{0}^{\tau}c_{KK}(\tau-u,Y(\tau),Y(u))du$$ 
    $$D_{Y}(\tau,Y) = qY\int_{0}^{\tau}c_{SS}(\tau-u,Y(\tau),Y(u))du + q\int_{0}^{\tau}p_{S}(\tau-u,Y(\tau),Y(u))du$$ 
    where $c_{SS}$ and $c_{KK}$ are second-order derivatives of the European call price .

The two boundaries are computed independently using these systems, and the intersection time $\tau^*$ is found afterward by solving for the point where $B(\tau^*) = Y(\tau^*)$ .

## 5. High-Performance Numerical Implementation

The efficiency of the Anderson engine stems from a combination of mathematical transformations and advanced numerical methods that allow the controlling parameters of the algorithm—the number of quadrature points ($l$), fixed-point iterations ($m$), and collocation points ($n$)—to be kept very small while maintaining high accuracy .

### 5.1 Collocation and Boundary Representation

The core of the method is to solve the fixed-point system not continuously, but only at a discrete set of $n$ collocation nodes $\{\tau_i\}_{i=1}^n$, and to represent the boundary between these nodes via polynomial interpolation .

* **Chebyshev Collocation**: To avoid the instability of high-order polynomial interpolation on equidistant grids (the Runge phenomenon), the collocation nodes are chosen to be the Chebyshev nodes . This placement is known to be near-optimal for polynomial approximation .

* **Boundary Transformation**: The exercise boundary $B(\tau)$ is not well-suited for direct polynomial interpolation due to its steepness and unbounded derivatives near $\tau=0$ . To overcome this, a variance-stabilizing transformation is applied. The interpolation is performed on the much smoother function $H(x)$:
    $$H(x) = \ln(B(x^2)/X)^2 \quad \text{where} \quad x=\sqrt{\tau} \quad \text{and} \quad X=K \min(1, r/q)$$ 
    This transformation, combining a logarithmic function with a square-root of time variable, creates a function that is nearly linear and exceptionally well-approximated by a low-degree Chebyshev polynomial, allowing for a very small number of collocation points ($n$) .

* **Polynomial Evaluation**: The interpolated polynomial is evaluated efficiently and stably at arbitrary points using the Clenshaw algorithm .

### 5.2 Numerical Quadrature

Accurate and fast computation of the integral operators in the fixed-point systems is essential.

* **Singularity Handling**: The FP-A system contains integrals with a weak singularity of the form $(\tau-u)^{-1/2}$ . This singularity is handled analytically via the variable transformation:
    $$z = \sqrt{\tau - u} \implies du = -2z \, dz$$ 
    This change of variables removes the singularity, resulting in a smooth integrand that can be integrated with high accuracy .

* **High-Order Quadrature**: The resulting smooth integrals are computed using high-performance quadrature rules, such as Gauss-Legendre or tanh-sinh quadrature . These spectral methods can achieve high precision with a very small number of quadrature nodes ($l$), often outperforming simpler schemes like the trapezoid rule by orders of magnitude .

### 5.3 Iteration Scheme Enhancements

The number of fixed-point iterations ($m$) required for convergence is minimized through two key techniques.

* **Jacobi-Newton Iteration**: To accelerate convergence, a Jacobi-Newton scheme is employed. This method uses the functional derivative of the fixed-point mapping to locally cancel out first-order error sensitivity, leading to quadratic convergence once the solution is sufficiently close to the true boundary . The iteration takes the form:
    $$B^{(j)}(\tau) = B^{(j-1)}(\tau) + \frac{B^{(j-1)}(\tau) - f(\tau, B^{(j-1)})}{f'(\tau, B^{(j-1)}) - 1}$$ 

* **High-Quality Initial Guess**: The iteration is initialized with a highly accurate analytical approximation of the boundary, such as the QD+ method, which reduces the number of required iterations significantly .

## 6. A Unified Framework

The Anderson pricing engine provides a unified mathematical architecture for handling both single and double boundary cases. The workflow is as follows:

1.  **Parameter Check**: Given the input parameters $(r, q)$, the engine first determines the appropriate boundary topology. For an American put:
    * If $q < r < 0$, the double-boundary case is triggered .
    * Otherwise, the single-boundary case is assumed.

2.  **Algorithm Selection**:
    * **Double-Boundary Case**: The engine employs the decoupled fixed-point systems (FP-B for the upper boundary $B$, FP-Y for the lower boundary $Y$) described in Section 4.4 . The boundaries are computed independently and checked for intersection to find $\tau^*$ .
    * **Single-Boundary Case**: The engine uses one of the standard fixed-point systems (FP-A or FP-B) from Section 3.3. FP-A is generally preferred for its faster convergence, except in strongly drift-dominated cases ($|r-q| \gg \sigma$) where FP-B offers greater stability .

3.  **Put-Call Symmetry**: American call options are priced using the fundamental put-call symmetry relationship . For a call option with parameters $(S, K, r, q, \sigma, \tau)$, the engine calculates the price of a put option with switched parameters $(K, S, q, r, \sigma, \tau)$. The call boundaries are then recovered via:
    $$B_{\text{call}}(\tau; K, r, q) = \frac{KS}{B_{\text{put}}(\tau; S, q, r)}, \quad Y_{\text{call}}(\tau; K, r, q) = \frac{KS}{Y_{\text{put}}(\tau; S, q, r)}$$ 

4.  **Final Price Calculation**: Once the boundary (or boundaries) are determined up to the desired maturity, the final option price is computed via high-order numerical integration of the appropriate integral formula (from Section 2.3 or 4.3) .

## 7. Conclusion

The mathematical architecture detailed in this paper provides a robust, highly efficient, and exceptionally accurate framework for the valuation of American options. By integrating spectral collocation, Chebyshev interpolation on transformed variables, high-order quadrature, and accelerated fixed-point iterations, the method achieves spectral convergence, drastically outperforming traditional numerical methods.

The key strengths of this mathematical design are:

1.  **Unified Approach**: It elegantly handles both standard single-boundary problems and the complex double-boundary cases arising from negative interest rates within a single, coherent framework.
2.  **Efficiency**: The combination of mathematical transformations and numerical techniques ensures that the algorithm's complexity is minimized, allowing for real-time computation without sacrificing precision.
3.  **Accuracy**: The spectral convergence properties enable the computation of option prices and their sensitivities to machine-level precision, a feat that is impractical with algebraic-convergence methods.
4.  **Robustness**: The development of decoupled iteration schemes for the double-boundary case and stable fixed-point systems like FP-B ensures the method is robust across a wide and challenging range of market parameters.

This engine architecture represents a significant advancement in computational finance, providing a definitive mathematical solution to the long-standing problem of American option pricing that is fit for the demands of modern financial markets.

## References

1.  Andersen, L. B., Lake, M., and Offengenden, D. 2016. High-performance American option pricing. *Journal of Computational Finance* 20(1), 39-87. 
2.  Andersen, L. and Lake, M. 2021. Fast American Option Pricing: The Double-Boundary Case. *Wilmott Magazine*, 2021(116), 32-40. 
3.  Barone-Adesi, G., and Whaley, R. 1987. Efficient Analytical Approximation of American Option Values. *Journal of Finance*, 42, 301-320. 
4.  Battauz, A., De Donno, M., and Sbuelz, A. 2015. Real options and American derivatives: The double continuation region. *Management Science* 61(5), 1094-1107. 
5.  Cortazar, G., Medina, L., and Naranjo, L. 2013. A Parallel Algorithm for Pricing American Options. *Working Paper, Pontificia Universidad Católica de Chile*. 
6.  Healy, J. 2021. Pricing American options under negative rates. *Journal of Computational Finance*, 25(1), 1-27. 
7.  Ikonen, S. and Toivanen, J. 2007. Pricing american options using lu decomposition. *Applied Mathematical Sciences* 1(51), 2529-2551. 
8.  Jaillet, P., Lamberton, D., and Lapeyre, B. 1990. Variational inequalities and the pricing of American options. *Acta Applicandae Mathematicae* 21(3), 263-289. 
9.  Ju, N. and Zhong, R. 1999. An approximate formula for pricing American Options. *Journal of Derivatives*, 7, 31-40. 
10. Kim, I. J. 1990. The analytic valuation of American options. *Review of Financial Studies* 3(4), 547-572. 
11. Kim, I., Jang, B.-G., and Kim, K. 2013. A simple iterative method for the valuation of American options. *Quantitative Finance*, 13(6), 885-895. 
12. Le Floc'h, F. 2022. Double sweep LU decomposition for American options under negative rates. *arXiv:2203.08794v1*. 
13. Li, M. 2010. Analytical approximations for the critical stock prices of American options: A performance comparison. *Review of Derivatives Research*, 13, 75-99. 
14. McDonald, R. and Schroder, M. 1998. A Parity Result for American Options. *Journal of Computational Finance*, 1, 5-13. 