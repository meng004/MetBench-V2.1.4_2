from numba import njit
def ceil(x: float) -> int:
    """
    Return the ceiling of x as an Integral.

    :param x: the number
    :return: the smallest integer >= x.

    # >>> import math
    # >>> all(ceil(n) == math.ceil(n) for n
    # ...     in (1, -1, 0, -0, 1.1, -1.1, 1.0, -1.0, 1_000_000_000))
    True
    """
    return int(x) if x - int(x) <= 0 else int(x) + 1

@njit
def _numba_ceil(x: float) -> int:
    """Numba兼容的ceil实现"""
    int_part = int(x)
    return int_part if x - int_part <= 0 else int_part + 1


# print(ceil(12.01))
def main(x: float) -> int:
    """
    Return the ceiling of x as an Integral.

    :param x: the number
    :return: the smallest integer >= x.

    # >>> import math
    # >>> all(ceil(n) == math.ceil(n) for n
    # ...     in (1, -1, 0, -0, 1.1, -1.1, 1.0, -1.0, 1_000_000_000))
    True
    """
    return _numba_ceil(x)

# print(ceil(12.01))

