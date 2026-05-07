def factorial(number: int) -> int:
    """
    Calculate the factorial of specified number (n!).

    # >>> import math
    # >>> all(factorial(i) == math.factorial(i) for i in range(20))
    # True
    # >>> factorial(0.1)
    # Traceback (most recent call last):
    #     ...
    # ValueError: factorial() only accepts integral values
    # >>> factorial(-1)
    # Traceback (most recent call last):
    #     ...
    # ValueError: factorial() not defined for negative values
    # >>> factorial(1)
    # 1
    # >>> factorial(6)
    # 720
    # >>> factorial(0)
    1
    """
    if number != int(number):
        raise ValueError("factorial() only accepts integral values")
    if number < 0:
        raise ValueError("factorial() not defined for negative values")
    value = 1
    for i in range(1, number + 1):
        value *= i
    return value

