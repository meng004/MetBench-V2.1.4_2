import math

def num_digits_fast(n: int) -> int:
    """
    Find the number of digits in a number.
    abs() is used as logarithm for negative numbers is not defined.

    # >>> num_digits_fast(12345)
    # 5
    # >>> num_digits_fast(123)
    # 3
    # >>> num_digits_fast(0)
    # 1
    # >>> num_digits_fast(-1)
    # 1
    # >>> num_digits_fast(-123456)
    # 6
    # >>> num_digits('123')  # Raises a TypeError for non-integer input
    Traceback (most recent call last):
        ...
    TypeError: Input must be an integer
    """

    if not isinstance(n, int):
        raise TypeError("Input must be an integer")

    return 1 if n == 0 else math.floor(math.log(abs(n), 10) + 1)