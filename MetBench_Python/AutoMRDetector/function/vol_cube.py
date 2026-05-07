from math import pi, pow


def vol_cube(side_length: float) -> float:
    """
    Calculate the Volume of a Cube.
    # >>> vol_cube(1)
    # 1.0
    # >>> vol_cube(3)
    # 27.0
    # >>> vol_cube(0)
    # 0.0
    # >>> vol_cube(1.6)
    # 4.096000000000001
    # >>> vol_cube(-1)
    Traceback (most recent call last):
        ...
    ValueError: vol_cube() only accepts non-negative values
    """
    if side_length < 0:
        raise ValueError("vol_cube() only accepts non-negative values")
    return pow(side_length, 3)