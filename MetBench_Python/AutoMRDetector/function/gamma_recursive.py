import math

from numpy import inf

def gamma_recursive(num: float) -> float:
    """
    Calculates the value of Gamma function of num
    where num is either an integer (1, 2, 3..) or a half-integer (0.5, 1.5, 2.5 ...).
    Implemented using recursion
    Examples:
    # >>> from math import isclose, gamma as math_gamma
    # >>> gamma_recursive(0.5)
    # 1.7724538509055159
    # >>> gamma_recursive(1)
    # 1.0
    # >>> gamma_recursive(2)
    # 1.0
    # >>> gamma_recursive(3.5)
    # 3.3233509704478426
    # >>> gamma_recursive(171.5)
    # 9.483367566824795e+307
    # >>> all(isclose(gamma_recursive(num), math_gamma(num))
    # ...     for num in (0.5, 2, 3.5, 171.5))
    # True
    # >>> gamma_recursive(0)
    Traceback (most recent call last):
        ...
    ValueError: math domain error
    # >>> gamma_recursive(-1.1)
    Traceback (most recent call last):
        ...
    ValueError: math domain error
    # >>> gamma_recursive(-4)
    Traceback (most recent call last):
        ...
    ValueError: math domain error
    # >>> gamma_recursive(172)
    # Traceback (most recent call last):
    #     ...
    # OverflowError: math range error
    # >>> gamma_recursive(1.1)
    Traceback (most recent call last):
        ...
    NotImplementedError: num must be an integer or a half-integer
    """
    if num <= 0:
        raise ValueError("math domain error")
    if num > 171.5:
        raise OverflowError("math range error")
    elif num - int(num) not in (0, 0.5):
        raise NotImplementedError("num must be an integer or a half-integer")
    elif num == 0.5:
        return math.sqrt(math.pi)
    else:
        return 1.0 if num == 1 else (num - 1) * gamma_recursive(num - 1)