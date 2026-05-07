from math import pi, sqrt, tan

def surface_area_sphere(radius: float) -> float:
    """
    Calculate the Surface Area of a Sphere.
    Wikipedia reference: https://en.wikipedia.org/wiki/Sphere
    Formula: 4 * pi * r^2

    # >>> surface_area_sphere(5)
    # 314.1592653589793
    # >>> surface_area_sphere(1)
    # 12.566370614359172
    # >>> surface_area_sphere(1.6)
    # 32.169908772759484
    # >>> surface_area_sphere(0)
    # 0.0
    # >>> surface_area_sphere(-1)
    Traceback (most recent call last):
        ...
    ValueError: surface_area_sphere() only accepts non-negative values
    """
    if radius < 0:
        raise ValueError("surface_area_sphere() only accepts non-negative values")
    return 4 * pi * radius**2