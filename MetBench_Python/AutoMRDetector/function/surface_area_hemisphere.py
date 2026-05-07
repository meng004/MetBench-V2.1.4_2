from math import pi, sqrt, tan

def surface_area_hemisphere(radius: float) -> float:
    """
    Calculate the Surface Area of a Hemisphere.
    Formula: 3 * pi * r^2

    # >>> surface_area_hemisphere(5)
    # 235.61944901923448
    # >>> surface_area_hemisphere(1)
    # 9.42477796076938
    # >>> surface_area_hemisphere(0)
    # 0.0
    # >>> surface_area_hemisphere(1.1)
    # 11.40398133253095
    # >>> surface_area_hemisphere(-1)
    Traceback (most recent call last):
        ...
    ValueError: surface_area_hemisphere() only accepts non-negative values
    """
    if radius < 0:
        raise ValueError("surface_area_hemisphere() only accepts non-negative values")
    return 3 * pi * radius**2