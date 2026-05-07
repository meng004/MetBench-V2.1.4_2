import math

def num_digits(n: int) -> int:
    """
    Find the number of digits in a number.

    # >>> num_digits(12345)
    # 5
    # >>> num_digits(123)
    # 3
    # >>> num_digits(0)
    # 1
    # >>> num_digits(-1)
    # 1
    # >>> num_digits(-123456)
    # 6
    # >>> num_digits('123')  # Raises a TypeError for non-integer input
    # Traceback (most recent call last):
        ...
    TypeError: Input must be an integer
    """

    if not isinstance(n, int):
        raise TypeError("Input must be an integer")

    digits = 0
    n = abs(n)
    while True:
        n = n // 10
        digits += 1
        if n == 0:
            break
    return digits