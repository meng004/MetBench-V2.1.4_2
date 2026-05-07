import math

def num_digits_faster(n: int) -> int:
    """
    Find the number of digits in a number.
    abs() is used for negative numbers

    # >>> num_digits_faster(12345)
    # 5
    # >>> num_digits_faster(123)
    # 3
    # >>> num_digits_faster(0)
    # 1
    # >>> num_digits_faster(-1)
    # 1
    # >>> num_digits_faster(-123456)
    # 6
    # >>> num_digits('123')  # Raises a TypeError for non-integer input
    Traceback (most recent call last):
        ...
    TypeError: Input must be an integer
    """

    if not isinstance(n, int):
        raise TypeError("Input must be an integer")

    return len(str(abs(n)))