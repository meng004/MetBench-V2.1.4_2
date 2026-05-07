def area_square(side_length: float) -> float:
    """
    Calculate the area of a square.

    # >>> area_square(10)
    # 100
    # >>> area_square(0)
    # 0
    # >>> area_square(1.6)
    # 2.5600000000000005
    # >>> area_square(-1)
    Traceback (most recent call last):
        ...
    ValueError: area_square() only accepts non-negative values
    """
    if side_length < 0:
        raise ValueError("area_square() only accepts non-negative values")
    return side_length**2
