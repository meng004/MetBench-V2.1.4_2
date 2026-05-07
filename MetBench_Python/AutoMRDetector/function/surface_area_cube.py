def surface_area_cube(side_length: float) -> float:
    """
    Calculate the Surface Area of a Cube.

    # >>> surface_area_cube(1)
    # 6
    # >>> surface_area_cube(1.6)
    # 15.360000000000003
    # >>> surface_area_cube(0)
    # 0
    # >>> surface_area_cube(3)
    # 54
    # >>> surface_area_cube(-1)
    Traceback (most recent call last):
        ...
    ValueError: surface_area_cube() only accepts non-negative values
    """
    if side_length < 0:
        raise ValueError("surface_area_cube() only accepts non-negative values")
    return 6 * side_length**2